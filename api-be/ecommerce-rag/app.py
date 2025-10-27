from flask import Flask, render_template, request, jsonify, session
from flask_cors import CORS
from flask_socketio import SocketIO, emit
import uuid
import requests
from threading import Thread
from config.config import Config
from services.rag_service import RAGService
from services.recommendation_api import RecommendationService
from services.product_context_kafka import ProductContextKafkaConsumer
from admin import admin_bp
import os

app = Flask(__name__)
app.config.from_object(Config)
CORS(app)
socketio = SocketIO(app, cors_allowed_origins="*")

# Initialize services
rag_service = RAGService()
recommendation_service = RecommendationService()

# Register blueprints
app.register_blueprint(admin_bp)

# Helper functions
def get_auth_token():
    """Lấy JWT token từ request"""
    token = request.args.get('token')
    if not token:
        token = request.cookies.get('access_token')
    if not token:
        token = session.get('token')
    return token

import requests

def call_csharp_api(endpoint, method='GET', data=None, json_data=None):
    """Gọi C# API"""
    token = get_auth_token()
    
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    
    url = f"{Config.CSHARP_API_BASE}/Order/{endpoint}"
    print(f"[DEBUG] {method} {url}")  # Log URL
    
    try:
        if method == 'POST':
            resp = requests.post(url, json=json_data, data=data, headers=headers, timeout=10, verify=False)
        elif method == 'PUT':
            resp = requests.put(url, json=json_data, data=data, headers=headers, timeout=10, verify=False)
        elif method == 'DELETE':
            resp = requests.delete(url, headers=headers, timeout=10, verify=False)
        elif method == 'GET':
            resp = requests.get(url, headers=headers, timeout=10, verify=False)
        else:
            return {'error': 'Invalid method'}, 400

        print(f"[DEBUG] Status: {resp.status_code}")
        print(f"[DEBUG] Raw Response: {resp.text[:500]}")  # log 500 ký tự đầu để xem API trả gì

        try:
            data = resp.json()
        except ValueError:
            return {'error': 'Invalid or empty JSON response', 'raw': resp.text}, resp.status_code

        if resp.status_code == 200:
            return data, 200
        else:
            return {'error': data.get('error', 'API error')}, resp.status_code
            
    except requests.exceptions.Timeout:
        return {'error': 'Request timeout'}, 504
    except requests.exceptions.RequestException as e:
        return {'error': f'Request failed: {str(e)}'}, 500


# Routes
@app.route('/')
def index():
    """Trang chat chính"""
    user_id = request.args.get('userId')
    token = request.args.get('token')
    
    if user_id:
        session['user_id'] = user_id
    if token:
        session['token'] = token
    if 'session_id' not in session:
        session['session_id'] = str(uuid.uuid4())
    
    return render_template('chat.html')

@app.route('/api/chat', methods=['POST'])
def chat():
    """API endpoint cho chat"""
    try:
        data = request.json
        query = data.get('query', '')
        session_id = session.get('session_id', 'default')
        user_token = get_auth_token()
        
        if not query:
            return jsonify({'error': 'Query is required'}), 400
        
        # Gọi RAG service với agent
        response = rag_service.chat(query, session_id, user_token)
        
        # Handle recommendations nếu cần
        if 'gợi ý' in query.lower() or 'recommend' in query.lower():
            recommendations = recommendation_service.get_recommendations(
                context=query,
                user_id=session_id
            )
            if recommendations:
                response['products'] = recommendations
                response['action'] = 'show_products'
                if 'answer' in response:
                    response['answer'] += "\n\nDựa trên nhu cầu của bạn, shop xin gợi ý thêm những sản phẩm sau:"
        
        return jsonify(response)
        
    except Exception as e:
        print(f"Error in chat endpoint: {e}")
        import traceback
        traceback.print_exc()
        return jsonify({
            'error': 'Internal server error',
            'message': str(e)
        }), 500

@app.route('/api/products/<int:product_id>', methods=['GET'])
def get_product(product_id):
    """Lấy thông tin chi tiết sản phẩm"""
    try:
        product = rag_service.get_product_details(product_id)
        if product:
            return jsonify(product)
        else:
            return jsonify({'error': 'Product not found'}), 404
    except Exception as e:
        print(f"Error getting product: {e}")
        return jsonify({'error': str(e)}), 500

# Cart endpoints - gọi C# API
@app.route('/api/cart', methods=['POST'])
def add_to_cart():
    """Thêm sản phẩm vào giỏ hàng qua C# API"""
    try:
        data = request.json
        product_id = data.get('product_id')
        quantity = data.get('quantity', 1)
        
        json_data = {'ProductId': product_id, 'Quantity': quantity}
        result, status = call_csharp_api('cart', method='POST', json_data=json_data)
        
        return jsonify(result), status
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/cart', methods=['GET'])
def get_cart():
    """Lấy giỏ hàng qua C# API"""
    try:
        result, status = call_csharp_api('detail-cart', method='GET')
        return jsonify(result), status
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/cart/remove/<int:product_id>', methods=['DELETE'])
def remove_from_cart(product_id):
    """Xóa sản phẩm khỏi giỏ qua C# API"""
    try:
        result, status = call_csharp_api(f'cart-remove?ProductId={product_id}', method='POST')
        return jsonify(result), status
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/cart/update', methods=['PUT'])
def update_cart():
    """Cập nhật giỏ hàng qua C# API"""
    try:
        data = request.json
        product_id = data.get('product_id')
        quantity = data.get('quantity', 1)
        
        json_data = {'ProductId': product_id, 'Quantity': quantity}
        result, status = call_csharp_api('cart', method='PUT', json_data=json_data)
        
        return jsonify(result), status
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/cart/clear', methods=['POST'])
def clear_cart():
    """Xóa giỏ hàng qua C# API"""
    try:
        result, status = call_csharp_api('cart/clear', method='POST')
        return jsonify(result), status
    except Exception as e:
        return jsonify({'error': str(e)}), 500

# WebSocket events
@socketio.on('connect')
def handle_connect():
    """Xử lý khi client kết nối"""
    print('✓ Client connected')
    emit('connected', {'message': 'Connected to server'})

@socketio.on('disconnect')
def handle_disconnect():
    """Xử lý khi client ngắt kết nối"""
    print('✗ Client disconnected')

@socketio.on('send_message')
def handle_message(data):
    """Xử lý tin nhắn từ client qua WebSocket"""
    try:
        query = data.get('message', '')
        session_id = session.get('session_id', 'default')
        user_token = get_auth_token()
        
        # Gọi RAG service
        response = rag_service.chat(query, session_id, user_token)
        
        # Handle recommendations
        if 'gợi ý' in query.lower() or 'recommend' in query.lower():
            recommendations = recommendation_service.get_recommendations(
                context=query,
                user_id=session_id
            )
            if recommendations:
                response['products'] = recommendations
                response['action'] = 'show_products'
        
        emit('receive_message', response)
        
    except Exception as e:
        print(f"WebSocket error: {e}")
        emit('error', {'message': str(e)})

if __name__ == '__main__':
    # Ensure upload folder exists
    os.makedirs(Config.UPLOAD_FOLDER, exist_ok=True)
    
    # Khởi tạo Kafka consumer cho auto-sync product context
    print("🚀 Starting Kafka consumer for product sync...")
    context_consumer = ProductContextKafkaConsumer()
    consumer_thread = Thread(target=context_consumer.execute, daemon=True)
    consumer_thread.start()
    
    print("\n" + "="*70)
    print("🎉 E-commerce RAG System Started with LangChain Tools & Agent!")
    print("="*70)
    print(f"✓ LangChain Agent: Initialized with {len(rag_service.agent.tools)} tools")
    print(f"✓ Kafka Consumer: Listening on {Config.KAFKA_TOPIC}")
    print(f"✓ Redis Cache: {Config.REDIS_HOST}:{Config.REDIS_PORT}")
    print(f"✓ Elasticsearch: {Config.ELASTICSEARCH_URL}")
    print(f"✓ Flask Server: http://0.0.0.0:5001")
    print("="*70 + "\n")
    
    # Run the app
    socketio.run(app, debug=True, host='0.0.0.0', port=5001)