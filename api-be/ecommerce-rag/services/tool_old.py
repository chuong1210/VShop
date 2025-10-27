"""
LangChain Tools cho RAG System
Không dùng MCP, dùng @tool decorator và Tool class của LangChain
"""
from langchain_core.tools import tool, Tool
from langchain.tools import StructuredTool

from typing import List, Dict, Optional
import json
import redis
import requests
from services.db_service import DatabaseService
from langchain_elasticsearch import ElasticsearchStore
from utils.embeddings import LocalEmbeddings
from utils.document_processor import DocumentProcessor
from config.config import Config
from pydantic import BaseModel

# Initialize services
db_service = DatabaseService()

# Redis client
redis_client = redis.StrictRedis(
    host=Config.REDIS_HOST,
    port=Config.REDIS_PORT,
    db=Config.REDIS_DB,
    decode_responses=True
)

# Elasticsearch vector store
embeddings = LocalEmbeddings(model_name="paraphrase-multilingual-MiniLM-L12-v2")
vector_store = ElasticsearchStore(
    es_url=Config.ELASTICSEARCH_URL,
    index_name=Config.ES_INDEX_NAME,
    embedding=embeddings
)

doc_processor = DocumentProcessor()
class GetProductsInput(BaseModel):
    category_id: Optional[int] = None
    limit: int = 10
# ==================== PRODUCT TOOLS ====================

@tool
def get_products_func(category_id: Optional[int] = None, limit: int = 10) -> str:
    """
    Lấy danh sách sản phẩm từ database với caching.
    
    Args:
        category_id: ID danh mục (optional), None để lấy tất cả
        limit: Số lượng sản phẩm tối đa (mặc định 10)
    
    Returns:
        JSON string chứa danh sách sản phẩm
    """
    try:
        # Tạo cache key
        cache_key = f"products:category:{category_id}:limit:{limit}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached
        
        # Cache MISS - query database
        print(f"✗ Cache MISS: {cache_key}")
        products = db_service.get_products(category_id=category_id, limit=limit)
        
        # Convert to JSON
        result = json.dumps(products, ensure_ascii=False)
        
        # Lưu vào cache (TTL 5 phút)
        redis_client.setex(cache_key, 300, result)
        
        return result
    except Exception as e:
        return json.dumps({"error": str(e)})
get_products = StructuredTool.from_function(
    func=get_products_func,
    name="get_products",
    description="Lấy danh sách sản phẩm từ database với caching.",
    args_schema=GetProductsInput
)
@tool
def search_products(search_term: str, limit: int = 10) -> str:
    """
    Tìm kiếm sản phẩm theo từ khóa với caching.
    
    Args:
        search_term: Từ khóa tìm kiếm (tên sản phẩm, mô tả)
        limit: Số lượng sản phẩm tối đa (mặc định 10)
    
    Returns:
        JSON string chứa danh sách sản phẩm tìm được
    """
    try:
        # Tạo cache key
        cache_key = f"products:search:{search_term}:limit:{limit}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached
        
        # Cache MISS
        print(f"✗ Cache MISS: {cache_key}")
        products = db_service.get_products(search_term=search_term, limit=limit)
        
        result = json.dumps(products, ensure_ascii=False)
        
        # Lưu vào cache (TTL 5 phút)
        redis_client.setex(cache_key, 300, result)
        
        return result
    except Exception as e:
        return json.dumps({"error": str(e)})

@tool
def get_product_details(product_id: int) -> str:
    """
    Lấy thông tin chi tiết của một sản phẩm với caching.
    
    Args:
        product_id: ID của sản phẩm cần xem
    
    Returns:
        JSON string chứa thông tin chi tiết sản phẩm
    """
    try:
        # Tạo cache key
        cache_key = f"product:{product_id}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached
        
        # Cache MISS
        print(f"✗ Cache MISS: {cache_key}")
        product = db_service.get_product_by_id(product_id)
        
        if product:
            result = json.dumps(product, ensure_ascii=False)
            # Lưu vào cache (TTL 10 phút)
            redis_client.setex(cache_key, 600, result)
            return result
        else:
            return json.dumps({"error": "Product not found"})
    except Exception as e:
        return json.dumps({"error": str(e)})

@tool
def get_categories() -> str:
    """
    Lấy danh sách tất cả danh mục sản phẩm với caching.
    
    Returns:
        JSON string chứa danh sách danh mục
    """
    try:
        cache_key = "categories:all"
        
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached
        
        print(f"✗ Cache MISS: {cache_key}")
        categories = db_service.get_categories()
        
        result = json.dumps(categories, ensure_ascii=False)
        
        # Lưu vào cache (TTL 1 giờ)
        redis_client.setex(cache_key, 3600, result)
        
        return result
    except Exception as e:
        return json.dumps({"error": str(e)})

@tool
def get_product_context_for_rag() -> str:
    """
    Lấy thông tin tổng quan về sản phẩm để sử dụng trong RAG context.
    Tool này trả về text mô tả các sản phẩm hiện có trong cửa hàng.
    
    Returns:
        String chứa thông tin sản phẩm dạng text
    """
    try:
        cache_key = "rag:product_context"
        
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached
        
        print(f"✗ Cache MISS: {cache_key}")
        context = db_service.get_products_for_rag_context()
        
        # Lưu vào cache (TTL 1 ngày)
        redis_client.setex(cache_key, 86400, context)
        
        return context
    except Exception as e:
        return f"Error getting product context: {str(e)}"

# ==================== DOCUMENT TOOLS ====================

@tool
def search_documents(query: str, k: int = 5) -> str:
    """
    Tìm kiếm tài liệu liên quan đến câu hỏi trong vector database.
    Dùng để tìm thông tin về cửa hàng, chính sách, hướng dẫn.
    
    Args:
        query: Câu hỏi hoặc từ khóa cần tìm
        k: Số lượng tài liệu trả về (mặc định 5)
    
    Returns:
        JSON string chứa danh sách tài liệu liên quan
    """
    try:
        retriever = vector_store.as_retriever(search_kwargs={"k": k})
        docs = retriever.invoke(query)
        
        results = []
        for doc in docs:
            results.append({
                "content": doc.page_content,
                "metadata": doc.metadata
            })
        
        return json.dumps(results, ensure_ascii=False)
    except Exception as e:
        return json.dumps([{"error": str(e)}])

@tool
def add_document_to_knowledge_base(file_path: str, doc_type: str = "general", description: str = "") -> str:
    """
    Thêm tài liệu mới vào knowledge base (vector database).
    Dùng để upload tài liệu Word, PDF về cửa hàng.
    
    Args:
        file_path: Đường dẫn đến file tài liệu
        doc_type: Loại tài liệu (general, policy, guide, product_info)
        description: Mô tả ngắn về tài liệu
    
    Returns:
        JSON string với kết quả thêm tài liệu
    """
    try:
        metadata = {
            "type": doc_type,
            "description": description
        }
        
        chunks = doc_processor.process_document(file_path, metadata)
        vector_store.add_documents(chunks)
        
        result = {
            "success": True,
            "message": f"Added {len(chunks)} chunks to knowledge base",
            "chunks_count": len(chunks)
        }
        
        return json.dumps(result)
    except Exception as e:
        return json.dumps({
            "success": False,
            "error": str(e)
        })

# ==================== CART TOOLS (Call C# API) ====================

def call_csharp_cart_api(endpoint: str, method: str = 'GET', json_data: dict = None, token: str = None) -> Dict:
    """Helper function để gọi C# Cart API"""
    try:
        headers = {'Content-Type': 'application/json'}
        if token:
            headers['Authorization'] = f'Bearer {token}'
        
        url = f"{Config.CSHARP_API_BASE}/Order/{endpoint}"
        
        if method == 'GET':
            response = requests.get(url, headers=headers, timeout=10)
        elif method == 'POST':
            response = requests.post(url, json=json_data, headers=headers, timeout=10)
        elif method == 'PUT':
            response = requests.put(url, json=json_data, headers=headers, timeout=10)
        elif method == 'DELETE':
            response = requests.delete(url, headers=headers, timeout=10)
        else:
            return {"error": "Invalid HTTP method"}
        
        if response.status_code == 200:
            return response.json()
        else:
            return {"error": f"API returned {response.status_code}"}
            
    except Exception as e:
        return {"error": str(e)}

@tool
def view_shopping_cart(user_token: str = "") -> str:
    """
    Xem giỏ hàng hiện tại của khách hàng.
    
    Args:
        user_token: JWT token của user (optional)
    
    Returns:
        JSON string chứa thông tin giỏ hàng
    """
    result = call_csharp_cart_api('cart', method='GET', token=user_token if user_token else None)
    return json.dumps(result, ensure_ascii=False)

@tool
def add_product_to_cart(product_id: int, quantity: int = 1, user_token: str = "") -> str:
    """
    Thêm sản phẩm vào giỏ hàng.
    
    Args:
        product_id: ID sản phẩm cần thêm
        quantity: Số lượng (mặc định 1)
        user_token: JWT token của user (optional)
    
    Returns:
        JSON string với kết quả thêm vào giỏ hàng
    """
    json_data = {'ProductId': product_id, 'Quantity': quantity}
    result = call_csharp_cart_api('cart', method='POST', json_data=json_data, token=user_token if user_token else None)
    return json.dumps(result, ensure_ascii=False)

# ==================== UTILITY TOOLS ====================

@tool
def invalidate_product_cache() -> str:
    """
    Xóa toàn bộ cache liên quan đến sản phẩm.
    Dùng khi có thay đổi sản phẩm trong database.
    
    Returns:
        JSON string với kết quả invalidation
    """
    try:
        # Xóa product list caches
        product_keys = redis_client.keys("products:*")
        if product_keys:
            redis_client.delete(*product_keys)
        
        # Xóa individual product caches
        individual_keys = redis_client.keys("product:*")
        if individual_keys:
            redis_client.delete(*individual_keys)
        
        # Xóa product context cache
        redis_client.delete("rag:product_context")
        
        # Xóa categories cache
        redis_client.delete("categories:all")
        
        result = {
            "success": True,
            "message": f"Invalidated {len(product_keys) + len(individual_keys) + 2} cache keys"
        }
        
        return json.dumps(result)
    except Exception as e:
        return json.dumps({"success": False, "error": str(e)})

# ==================== EXPORT ALL TOOLS ====================

ALL_TOOLS = [
    # Product tools
    get_products,
    search_products,
    get_product_details,
    get_categories,
    get_product_context_for_rag,
    
    # Document tools
    search_documents,
    add_document_to_knowledge_base,
    
    # Cart tools
    view_shopping_cart,
    add_product_to_cart,
    
    # Utility tools
    invalidate_product_cache
]

