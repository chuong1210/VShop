
from flask import Flask, request, jsonify
from flask_cors import CORS
app = Flask(__name__)
CORS(app)  # Enable CORS for .NET API
import logging
from BigData_training.recommendation_service import recommender

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
import datetime
@app.route('/smw-api/recommendations/collaborative', methods=['GET'])
def collaborative_recommendations():
    """Collaborative Filtering recommendations"""
    try:
        user_id = int(request.args.get('userId'))
        num = int(request.args.get('num', 10))
        
        recommendations = recommender.collaborative_filtering_recommendations(user_id, num)
        
        return jsonify({
            'success': True,
            'data': recommendations,
            'total': len(recommendations)
        }), 200
    
    except Exception as e:
        logger.error(f"Error in CF endpoint: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/recommendations/similar/<int:product_id>', methods=['GET'])
def similar_products(product_id):
    """Content-Based similar products"""
    try:
        num = int(request.args.get('num', 10))
        
        recommendations = recommender.content_based_recommendations(product_id, num)
        
        return jsonify({
            'success': True,
            'data': recommendations,
            'total': len(recommendations)
        }), 200
    
    except Exception as e:
        logger.error(f"Error in CB endpoint: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/recommendations/hybrid', methods=['GET'])
def hybrid_recommendations():
    """Hybrid recommendations (CF + CB)"""
    try:
        user_id = int(request.args.get('userId'))
        num = int(request.args.get('num', 10))
        cf_weight = float(request.args.get('cfWeight', 0.6))
        cb_weight = float(request.args.get('cbWeight', 0.4))
        
        recommendations = recommender.hybrid_recommendations(
            user_id, num, cf_weight, cb_weight
        )
        
        return jsonify({
            'success': True,
            'data': recommendations,
            'total': len(recommendations)
        }), 200
    
    except Exception as e:
        logger.error(f"Error in hybrid endpoint: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/product/search', methods=['GET'])
def search_products():
    """Text search products"""
    try:
        search_keyword = request.args.get('searchKeyword', '')
        page = int(request.args.get('page', 1))
        page_size = int(request.args.get('pageSize', 20))
        
        if not search_keyword:
            return jsonify({
                'success': False,
                'error': 'Search keyword is required'
            }), 400
        
        # Calculate offset
        offset = (page - 1) * page_size
        
        # Get search results
        all_results = recommender.text_search_recommendations(
            search_keyword, 
            num=page_size * 3  # Get more for pagination
        )
        
        # Paginate
        total = len(all_results)
        paginated_results = all_results[offset:offset + page_size]
        
        return jsonify({
            'success': True,
            'data': paginated_results,
            'page': page,
            'pageSize': page_size,
            'total': total,
            'totalPages': (total + page_size - 1) // page_size
        }), 200
    
    except Exception as e:
        logger.error(f"Error in search endpoint: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/recommendations/track', methods=['POST'])
def track_interaction():
    """Track user interaction"""
    try:
        data = request.get_json()
        user_id = data.get('userId')
        product_id = data.get('productId')
        interaction_type = data.get('type', 'view')  # view, purchase, cart
        
        success = recommender.track_user_interaction(
            user_id, product_id, interaction_type
        )
        
        return jsonify({
            'success': success,
            'message': 'Interaction tracked'
        }), 200
    
    except Exception as e:
        logger.error(f"Error tracking interaction: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/recommendations/trending', methods=['GET'])
def trending_products():
    """Get trending products from Redis"""
    try:
        limit = int(request.args.get('limit', 10))
        trending_type = request.args.get('type', 'views')  # views or purchases
        
        key = f'trending:{trending_type}'
        trending = recommender.redis_client.zrevrange(key, 0, limit - 1, withscores=True)
        
        results = []
        for product_id_str, score in trending:
            product_id = int(product_id_str)
            product = recommender.get_product_details(product_id)
            if product:
                results.append({
                    'productId': product_id,
                    'score': int(score),
                    'product': product,
                    'trendingType': trending_type
                })
        
        return jsonify({
            'success': True,
            'data': results,
            'total': len(results)
        }), 200
    
    except Exception as e:
        logger.error(f"Error getting trending: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/smw-api/product/index', methods=['POST'])
def index_products():
    """Index all products to Elasticsearch"""
    try:
        conn = recommender.get_sql_connection()
        cursor = conn.cursor()
        
        # Fetch all products
        cursor.execute("""
            SELECT p.Id, p.Name, p.Images, p.Price, p.Describes, 
                   p.Feature, p.Specifications, p.CategoryId, 
                   c.Name as CategoryName
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.IsDeleted = 0 AND p.Status = 1
        """)
        
        products = cursor.fetchall()
        conn.close()
        
        # Create index if not exists
        index_name = 'products'
        if not recommender.es.indices.exists(index=index_name):
            mapping = {
                "mappings": {
                    "properties": {
                        "Id": {"type": "integer"},
                        "Name": {
                            "type": "text",
                            "fields": {"keyword": {"type": "keyword"}}
                        },
                        "Images": {"type": "keyword"},
                        "Price": {"type": "float"},
                        "Describes": {"type": "text"},
                        "Feature": {"type": "text"},
                        "Specifications": {"type": "text"},
                        "CategoryId": {"type": "integer"},
                        "CategoryName": {
                            "type": "text",
                            "fields": {"keyword": {"type": "keyword"}}
                        },
                        "model_factor": {
                            "type": "dense_vector",
                            "dims": 25
                        }
                    }
                }
            }
            recommender.es.indices.create(index=index_name, body=mapping)
        
        # Index products
        indexed_count = 0
        for row in products:
            doc = {
                'Id': row[0],
                'Name': row[1],
                'Images': row[2].split(',') if row[2] else [],
                'Price': float(row[3]) if row[3] else 0,
                'Describes': row[4],
                'Feature': row[5],
                'Specifications': row[6],
                'CategoryId': row[7],
                'CategoryName': row[8]
            }
            
            recommender.es.index(index=index_name, id=row[0], body=doc)
            indexed_count += 1
        
        return jsonify({
            'success': True,
            'message': f'Indexed {indexed_count} products',
            'total': indexed_count
        }), 200
    
    except Exception as e:
        logger.error(f"Error indexing products: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    try:
        # Check Elasticsearch
        es_ok = recommender.es.ping()
        
        # Check MongoDB
        mongo_ok = recommender.mongo_client.server_info() is not None
        
        # Check Redis
        redis_ok = recommender.redis_client.ping()
        
        # Check SQL Server
        try:
            conn = recommender.get_sql_connection()
            conn.close()
            sql_ok = True
        except:
            sql_ok = False
        
        status = 'healthy' if all([es_ok, mongo_ok, redis_ok, sql_ok]) else 'degraded'
        
        return jsonify({
            'status': status,
            'services': {
                'elasticsearch': 'ok' if es_ok else 'failed',
                'mongodb': 'ok' if mongo_ok else 'failed',
                'redis': 'ok' if redis_ok else 'failed',
                'sqlserver': 'ok' if sql_ok else 'failed',
                'models': 'loaded' if recommender.als_model else 'not_loaded'
            },
            'timestamp': datetime.now().isoformat()
        }), 200
    
    except Exception as e:
        return jsonify({
            'status': 'error',
            'error': str(e)
        }), 500


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
