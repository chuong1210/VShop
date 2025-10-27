"""
Hybrid Recommendation Service for .NET Core Integration
Combines: ALS (Collaborative Filtering) + Elasticsearch (Content-Based) + Text Search
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
from pyspark.sql import SparkSession
from pyspark.ml.recommendation import ALSModel
from pyspark.ml import PipelineModel
from elasticsearch import Elasticsearch
import pymongo
import pyodbc
import redis
import json
from datetime import datetime
import logging
from pydantic import BaseModel, Field
from pyspark.sql.types import StructType, StructField, IntegerType, DoubleType
from typing import List, Dict, Any, Optional
# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class HybridRecommendationService:
    def __init__(self, redis_client, es_client, spark):
        self.redis_client = redis_client
        self.es_client = es_client
        self.spark = spark
        
        # Load trained models
        try:
            self.als_model = ALSModel.load("/models/als_model")
            self.indexer_model = PipelineModel.load("/models/indexer_model")
            logger.info("Models loaded successfully")
        except Exception as e:
            logger.error(f"Error loading models: {e}")
            self.als_model = None
            self.indexer_model = None
    
    def get_user_index(self, user_id: int) -> Optional[int]:
        """Get user index from indexer"""
        if not self.indexer_model:
            return None
        # Create a temp DF for indexing
        schema = StructType([StructField("userId", IntegerType(), True)])
        df = self.spark.createDataFrame([(user_id,)], schema)
        indexed_df = self.indexer_model.stages[0].transform(df)  # User indexer is first
        row = indexed_df.collect()[0]
        return int(row['userId_index']) if row['userId_index'] is not None else None
    
    def get_product_index(self, product_id: int) -> Optional[int]:
        """Get product index from indexer"""
        if not self.indexer_model:
            return None
        schema = StructType([StructField("productId", IntegerType(), True)])
        df = self.spark.createDataFrame([(product_id,)], schema)
        indexed_df = self.indexer_model.stages[1].transform(df)  # Product indexer is second
        row = indexed_df.collect()[0]
        return int(row['productId_index']) if row['productId_index'] is not None else None
    
    def get_collaborative_recommendations(self, user_id: int, num_recs: int = 10) -> List[int]:
        """Get collaborative recommendations using ALS"""
        if not self.als_model:
            logger.warning("ALS model not available, returning empty recs")
            return []
        
        user_idx = self.get_user_index(user_id)
        if user_idx is None:
            return []
        
        # Recommend products
        recs_df = self.als_model.recommendForUserSubset(
            self.spark.createDataFrame([(user_idx,)], StructType([StructField("userId_index", IntegerType(), True)])),
            num_recs
        )
        recs = recs_df.collect()[0]['recommendations'] if recs_df.count() > 0 else []
        
        # Extract product indices and map back to original IDs (simplified; in prod, load labels)
        product_indices = [int(r['productId_index']) for r in recs]
        # For mapping back, we'd need to reverse the indexer labels - assuming we load them or use a cache
        # Placeholder: Return indices as IDs for now; enhance with label array if needed
        return product_indices  # In full impl, map to original product IDs
    
    def get_similar_products(self, product_id: int, num_recs: int = 10) -> List[Dict[str, Any]]:
        """Get similar products using Elasticsearch KNN"""
        # First, get the product's embedding
        res = self.es_client.get(index="products", id=product_id)
        if not res or '_source' not in res:
            return []
        product_embedding = res['_source'].get('embedding', [])
        
        search_body = {
            "knn": {
                "field": "embedding",
                "query_vector": product_embedding,
                "k": num_recs,
                "num_candidates": 50
            },
            "_source": ["id", "name", "price", "images"],
            "min_score": 0.7
        }
        
        response = self.es_client.search(index="products", body=search_body)
        return [hit['_source'] for hit in response['hits']['hits'] if hit['_source']['id'] != product_id]
    
    def get_hybrid_recommendations(self, user_id: int, num_recs: int = 10) -> List[Dict[str, Any]]:
        """Combine collaborative and content-based recs"""
        collab_recs = self.get_collaborative_recommendations(user_id, num_recs)
        # For hybrid, fetch details for collab recs via ES
        content_recs = self.get_similar_products(user_id, num_recs)  # Or base on user's last viewed; simplified
        
        # Simple merge: Take unique, score by source
        hybrid = []
        for rec in content_recs:
            rec['score'] = 0.6  # Content weight
            hybrid.append(rec)
        # Add collab (placeholder mapping)
        for rec_id in collab_recs[:5]:  # Limit
            hybrid.append({'id': rec_id, 'score': 0.4})  # Collab weight
        
        # Sort by score (enhance with full logic)
        hybrid.sort(key=lambda x: x.get('score', 0), reverse=True)
        return hybrid[:num_recs]
    
    def track_user_interaction(self, user_id: int, product_id: int, interaction_type: str) -> bool:
        """Track user interactions for real-time updates"""
        try:
            timestamp = datetime.now().isoformat()
            interaction = {
                'userId': user_id,
                'productId': product_id,
                'type': interaction_type,
                'timestamp': timestamp
            }
            
            # Add to user history (sorted set by timestamp)
            self.redis_client.zadd(
                f'user:history:{user_id}',
                {str(product_id): datetime.now().timestamp()}
            )
            
            # Track product interactions
            if interaction_type == 'view':
                self.redis_client.zincrby('trending:views', 1, str(product_id))
            elif interaction_type == 'purchase':
                self.redis_client.zincrby('trending:purchases', 3, str(product_id))  # Higher weight for purchases
            elif interaction_type == 'like':
                self.redis_client.zincrby('trending:likes', 2, str(product_id))
            
            logger.info(f"Tracked interaction: {interaction}")
            return True
        
        except Exception as e:
            logger.error(f"Error tracking interaction: {e}")
            return False
    
    def get_trending_products(self, num_recs: int = 10) -> List[Dict[str, Any]]:
        """Get trending products from Redis"""
        try:
            # Combine views and purchases
            views = self.redis_client.zrevrange('trending:views', 0, num_recs-1, withscores=True)
            purchases = self.redis_client.zrevrange('trending:purchases', 0, num_recs-1, withscores=True)
            
            # Simple merge: score = views_score * 0.3 + purchases_score * 0.7
            trends = {}
            for pid, score in views:
                trends[str(pid)] = score * 0.3
            for pid, score in purchases:
                pid_str = str(pid)
                trends[pid_str] = trends.get(pid_str, 0) + score * 0.7
            
            # Sort and get top
            sorted_trends = sorted(trends.items(), key=lambda x: x[1], reverse=True)[:num_recs]
            
            # Fetch details from ES
            product_ids = [int(pid) for pid, _ in sorted_trends]
            if not product_ids:
                return []
            
            search_body = {"query": {"terms": {"id": product_ids}}, "_source": ["id", "name", "price", "images"]}
            response = self.es_client.search(index="products", body=search_body)
            products = {hit['_source']['id']: hit['_source'] for hit in response['hits']['hits']}
            
            return [products.get(pid, {}) for pid, _ in sorted_trends]
        except Exception as e:
            logger.error(f"Error getting trending: {e}")
            return []