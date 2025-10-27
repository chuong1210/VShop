"""
Train ALS Model for Product Recommendation
Data from: SQL Server (Products) + MongoDB (Reviews)
"""

from pyspark.sql import SparkSession
from pyspark.sql.types import StructType, StructField, IntegerType, DoubleType
from pyspark.ml.recommendation import ALS
from pyspark.ml.evaluation import RegressionEvaluator
from pyspark.ml.feature import StringIndexer
from pyspark.ml import Pipeline
from pyspark.sql.functions import col, lit, unix_timestamp, current_timestamp
import pymongo
import pyodbc
import logging
from datetime import datetime  # For timestamp
from pyspark.ml.linalg import DenseVector  # For type checking

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
import os

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODEL_DIR = os.path.join(BASE_DIR, "models")
os.makedirs(MODEL_DIR, exist_ok=True)

class ModelTrainer:
    def __init__(self):
        # Initialize Spark
        self.spark = SparkSession.builder \
            .appName("ProductRecommendationTraining") \
            .master("local[*]") \
            .config("spark.driver.memory", "4g") \
            .config("spark.executor.memory", "4g") \
            .getOrCreate()
        
        # MongoDB connection
        self.mongo_client = pymongo.MongoClient('mongodb://root:admin123@localhost:27017/')
        self.mongo_db = self.mongo_client['api_be_db']
        
        # SQL Server connection (aligned with data generator)
        self.sql_conn_str = (
            "DRIVER={ODBC Driver 17 for SQL Server};"
            "SERVER=USER\\MSSQLSERVER01;"
            "DATABASE=SPMK_VSHOP;"
            "UID=sa;"
            "PWD=101204;"
            "TrustServerCertificate=yes;"
        )
        self.sql_conn = None  # Initialize lazily if needed
    
    def load_reviews_from_mongodb(self):
        """Load reviews from MongoDB and convert to Spark DataFrame"""
        logger.info("Loading reviews from MongoDB...")
        
        # Get reviews from MongoDB
        reviews = list(self.mongo_db.productReviews.find(
            {'isDeleted': False, 'isApproved': True},
            {'productId': 1, 'userId': 1, 'rating': 1}
        ))
        
        logger.info(f"Loaded {len(reviews)} reviews from MongoDB")
        
        # Convert to Spark DataFrame
        review_data = [(r['userId'], r['productId'], float(r['rating'])) 
                       for r in reviews]
        
        schema = StructType([
            StructField("userId", IntegerType(), True),
            StructField("productId", IntegerType(), True),
            StructField("rating", DoubleType(), True)
        ])
        
        df = self.spark.createDataFrame(review_data, schema)
        
        # Remove duplicates (same user rating same product multiple times)
        df = df.dropDuplicates(['userId', 'productId'])
        
        logger.info(f"Created Spark DataFrame with {df.count()} unique ratings")
        return df
    
    def prepare_data(self, df):
        """Prepare data: encode IDs and split train/test"""
        logger.info("Preparing data...")
        
        # String indexers to convert IDs to indices
        user_indexer = StringIndexer(
            inputCol="userId",
            outputCol="userId_index",
            handleInvalid='keep'
        )
        
        product_indexer = StringIndexer(
            inputCol="productId",
            outputCol="productId_index",
            handleInvalid='keep'
        )
        
        # Create pipeline for indexing
        indexer_pipeline = Pipeline(stages=[user_indexer, product_indexer])
        
        # Fit and transform
        indexer_model = indexer_pipeline.fit(df)
        df_indexed = indexer_model.transform(df)
        
        # Save indexer model for later use
        indexer_model.write().overwrite().save("/models/indexer_model")
        indexer_model.write().overwrite().save(os.path.join(MODEL_DIR, "indexer_model"))
        logger.info("Saved indexer model")
        
        # Show data statistics
        logger.info(f"Unique users: {df_indexed.select('userId').distinct().count()}")
        logger.info(f"Unique products: {df_indexed.select('productId').distinct().count()}")
        logger.info(f"Total ratings: {df_indexed.count()}")
        
        # Rating distribution
        logger.info("Rating distribution:")
        df_indexed.groupBy('rating').count().orderBy('rating').show()
        
        return df_indexed, indexer_model
    
    def train_als_model(self, df, max_iter=10, reg_param=0.01, rank=25):
        """Train ALS model"""
        logger.info("Training ALS model...")
        
        # Split data
        train_data, test_data = df.randomSplit([0.8, 0.2], seed=42)
        
        logger.info(f"Training set: {train_data.count()} ratings")
        logger.info(f"Test set: {test_data.count()} ratings")
        
        # Configure ALS
        als = ALS(
            maxIter=max_iter,
            regParam=reg_param,
            rank=rank,
            userCol="userId_index",
            itemCol="productId_index",
            ratingCol="rating",
            coldStartStrategy="drop",  # Drop users/items not seen in training
            nonnegative=True  # Non-negative factors for better interpretability
        )
        
        # Train model
        logger.info(f"Training with params: maxIter={max_iter}, regParam={reg_param}, rank={rank}")
        model = als.fit(train_data)
        
        # Evaluate on test set
        predictions = model.transform(test_data)
        predictions = predictions.na.drop()
        
        # Calculate RMSE
        evaluator_rmse = RegressionEvaluator(
            metricName="rmse",
            labelCol="rating",
            predictionCol="prediction"
        )
        rmse = evaluator_rmse.evaluate(predictions)
        
        # Calculate MAE
        evaluator_mae = RegressionEvaluator(
            metricName="mae",
            labelCol="rating",
            predictionCol="prediction"
        )
        mae = evaluator_mae.evaluate(predictions)
        
        logger.info(f"Test RMSE: {rmse:.4f}")
        logger.info(f"Test MAE: {mae:.4f}")
        
        # Baseline metrics
        avg_rating = train_data.agg({"rating": "avg"}).collect()[0][0]
        logger.info(f"Average rating: {avg_rating:.4f}")
        
        # Show sample predictions
        logger.info("Sample predictions:")
        predictions.select("userId", "productId", "rating", "prediction").show(10)
        
        return model, rmse, mae
    
    def save_model(self, model, rmse, mae):
        """Save trained model and metadata"""
        logger.info("Saving model...")
        
        # Save ALS model
        model.write().overwrite().save("/models/als_model")
        model.write().overwrite().save(os.path.join(MODEL_DIR, "als_model"))

        logger.info("Saved ALS model to /models/als_model")
        
        # Save metadata
        import json
        metadata = {
            'model_type': 'ALS',
            'rank': model.rank,
            'rmse': float(rmse),
            'mae': float(mae),
            'trained_at': datetime.utcnow().isoformat(),
            'num_user_factors': model.userFactors.count(),
            'num_item_factors': model.itemFactors.count()
        }
        
        # with open('/models/model_metadata.json', 'w') as f:
        #     json.dump(metadata, f, indent=2)
        with open(os.path.join(MODEL_DIR, "model_metadata.json"), 'w') as f:
            json.dump(metadata, f, indent=2)
        
        logger.info("Saved model metadata")
    
    def extract_item_factors_for_elasticsearch(self, model, indexer_model):
        """Extract item (product) factors for Elasticsearch indexing"""
        logger.info("Extracting item factors...")
        
        try:
            # Get item factors
            item_factors = model.itemFactors
            
            # Get product indexer to map back to original IDs
            product_indexer = indexer_model.stages[1]
            product_labels = product_indexer.labels
            
            # Convert to list
            factors = item_factors.collect()
            
            product_vectors = []
            for row in factors:
                idx = int(row['id'])
                if idx < len(product_labels):
                    product_id = product_labels[idx]
                    
                    # FIXED: Handle both DenseVector and list
                    features = row['features']
                    if isinstance(features, DenseVector):
                        vector = features.toArray().tolist()
                    else:
                        # Already a list (common after .collect())
                        vector = list(features)  # Ensure it's a list
                        logger.debug(f"Features already list for product {product_id}: {type(features)}")
                    
                    product_vectors.append({
                        'productId': product_id,
                        'vector': vector
                    })
            
            logger.info(f"Extracted {len(product_vectors)} product vectors")
            
            # Save to file for Elasticsearch indexing
            import json
            # with open('/models/product_vectors.json', 'w') as f:
            #     json.dump(product_vectors, f)
            
            with open(os.path.join(MODEL_DIR, "product_vectors.json"), 'w') as f:
                json.dump(product_vectors, f)
            logger.info("Saved product vectors to /models/product_vectors.json")
            
            return product_vectors
        except Exception as e:
            logger.error(f"Error extracting item factors: {e}")
            return []
    
    def run(self):
        """Run complete training pipeline"""
        logger.info("="*50)
        logger.info("Starting Model Training Pipeline")
        logger.info("="*50)
        
        try:
            # Step 1: Load data
            df = self.load_reviews_from_mongodb()
            
            # Step 2: Prepare data
            df_prepared, indexer_model = self.prepare_data(df)
            
            # Step 3: Train model
            model, rmse, mae = self.train_als_model(
                df_prepared,
                max_iter=10,
                reg_param=0.01,
                rank=25
            )
            
            # Step 4: Save model
            self.save_model(model, rmse, mae)
            
            # Step 5: Extract item factors
            self.extract_item_factors_for_elasticsearch(model, indexer_model)
            
            logger.info("="*50)
            logger.info("Training completed successfully!")
            logger.info("="*50)
            
            return True
        
        except Exception as e:
            logger.error(f"Error in training pipeline: {e}")
            import traceback
            traceback.print_exc()
            return False
    
    def close(self):
        """Clean up resources"""
        self.spark.stop()
        self.mongo_client.close()
        if self.sql_conn:
            self.sql_conn.close()


if __name__ == "__main__":
    trainer = ModelTrainer()
    
    try:
        success = trainer.run()
        exit(0 if success else 1)
    finally:
        trainer.close()