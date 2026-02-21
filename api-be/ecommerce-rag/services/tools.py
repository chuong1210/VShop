

"""
LangChain Tools cho RAG System - Phiên bản hoàn thiện với StructuredTool cho multi-arg tools
"""
from typing import List, Dict, Optional
import json
import redis
import requests

from services.db_service import DatabaseService
from langchain_elasticsearch import ElasticsearchStore
from utils.embeddings import LocalEmbeddings
from utils.document_processor import DocumentProcessor
from config.config import Config
from langchain_core.pydantic_v1 import BaseModel,Field

# LangChain tool classes
from langchain_core.tools import tool  # giữ để dùng với tools 1-arg
from langchain.tools import StructuredTool

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

# ----------------- Pydantic schemas for StructuredTool -----------------

class GetProductsInput(BaseModel):
    category_id: Optional[int] = Field(default=None, description="ID danh mục (optional, None cho tất cả)")
    limit: int = Field(default=10, description="Số lượng sản phẩm tối đa")

class SearchProductsInput(BaseModel):
    search_term: str
    limit: int = 10

class AddToCartInput(BaseModel):
    product_id: int = Field(..., gt=0, description="ID sản phẩm (phải là số nguyên >0)")
    quantity: int = Field(default=1, ge=1)
    user_token: Optional[str] = None

class SearchDocumentsInput(BaseModel):
    query: str
    k: int = 5

class AddDocumentInput(BaseModel):
    file_path: str
    doc_type: str = "general"
    description: str = ""

# ==================== PRODUCT TOOLS (Structured where needed) ====================

def _cache_key_for_products(category_id: Optional[int], limit: int) -> str:
    return f"products:category:{category_id}:limit:{limit}"

def _cache_key_for_search(search_term: str, limit: int) -> str:
    return f"products:search:{search_term}:limit:{limit}"

# get_products (multi-arg -> StructuredTool)
# @tool
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
        cache_key = _cache_key_for_products(category_id, limit)
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached

        print(f"✗ Cache MISS: {cache_key}")
        products = db_service.get_products(category_id=category_id, limit=limit)

        result = json.dumps(products, ensure_ascii=False)
        redis_client.setex(cache_key, 300, result)
        return result
    except Exception as e:
        return json.dumps({"error": str(e)})

get_products = StructuredTool.from_function(
    func=get_products_func,
    name="get_products",
    description="Lấy danh sách sản phẩm từ database với caching. Args: category_id (int|optional, None cho tất cả), limit (int). Output: JSON list sản phẩm.",    args_schema=GetProductsInput
)

# search_products (multi-arg -> StructuredTool)
# @tool  # Giữ decorator nếu dùng
def search_products_func(search_term: str, limit: int = 10) -> str:
    """
    Tìm kiếm sản phẩm theo từ khóa với caching via API call.
    
    Args:
        search_term: Từ khóa tìm kiếm (tên sản phẩm, mô tả)
        limit: Số lượng sản phẩm tối đa (mặc định 10)
    
    Returns:
        JSON string chứa danh sách sản phẩm tìm được (simple list, không pagination)
    """
    try:
        # Cache key (tái sử dụng logic cũ)
        cache_key = _cache_key_for_search(search_term, limit)
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached

        print(f"✗ Cache MISS: {cache_key}")
        
        # Config API (thay bằng env var hoặc Config)
        API_BASE = "http://localhost:5000"  
        url = f"{API_BASE}/smw-api/product/search"
        params = {
            'searchKeyword': search_term,
            'pageSize': limit,
            'page': 1  # Chỉ lấy page 1, vì tool không cần full pagination
        }
        
        # Gọi API (timeout 10s để tránh hang)
        response = requests.get(url, params=params, timeout=10)
        response.raise_for_status()  # Raise nếu != 200
        
        api_data = response.json()
        
        # Extract data từ PaginatedResult (theo schema Flask của bạn)
        if api_data.get('succeeded', False):
            products_raw = api_data.get('data', [])
        else:
            return json.dumps({"error": api_data.get('messages', ['API search failed'])})
        
        # Flatten thành simple list (loại bỏ extra, chỉ giữ fields cần cho RAG/tool)
        simple_products = []
        for p in products_raw:
            simple_p = {
                'id': p.get('Id'),
                'internal_code': p.get('InternalCode'),
                'name': p.get('Name'),
                'images': p.get('Images', []),
                'price': p.get('Price'),
                'new_price': p.get('NewPrice'),  # Đã tính promo từ API
                'quantity': p.get('Quantity'),
                'describes': p.get('Describes'),
                'feature': p.get('Feature'),
                'specifications': p.get('Specifications'),
                'status': p.get('Status'),
                'parent_id': p.get('ParentId'),
                'category_id': p.get('CategoryId'),
                'category': p.get('Category', {}),  # Có Name
                'promotion': p.get('PromotionDto')  # Full promo info
            }
            simple_products.append(simple_p)
        
        result = json.dumps(simple_products, ensure_ascii=False)
        
        # Cache kết quả (5 phút)
        redis_client.setex(cache_key, 300, result)
        return result
        
    except requests.exceptions.RequestException as e:
        print(f"API call failed: {e}")
        # Fallback to old db_service nếu API down (tùy chọn)
        # products = db_service.get_products(search_term=search_term, limit=limit)
        # return json.dumps(products, ensure_ascii=False)
        return json.dumps({"error": f"Search API error: {str(e)}"})
    
    except Exception as e:
        print(f"Unexpected error: {e}")
        return json.dumps({"error": str(e)})
search_products = StructuredTool.from_function(
    func=search_products_func,
    name="search_products",
    description="Tìm kiếm sản phẩm theo từ khóa với caching. Args: search_term (str), limit (int).",
    args_schema=SearchProductsInput
)

# get_product_details - single arg -> giữ @tool (dễ dùng)
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
        cache_key = f"product:{product_id}"
        cached = redis_client.get(cache_key)
        if cached:
            print(f"✓ Cache HIT: {cache_key}")
            return cached

        print(f"✗ Cache MISS: {cache_key}")
        product = db_service.get_product_by_id(product_id)
        if product:
            result = json.dumps(product, ensure_ascii=False)
            redis_client.setex(cache_key, 600, result)
            return result
        else:
            return json.dumps({"error": "Product not found"})
    except Exception as e:
        return json.dumps({"error": str(e)})

@tool("get_categories", args_schema=None)
def get_categories(dummy_input: str = "") -> str:
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
        redis_client.setex(cache_key, 3600, result)
        return result
    except Exception as e:
        return json.dumps({"error": str(e)})
from langchain_core.pydantic_v1 import BaseModel, Field

class EmptyArgs(BaseModel):
    """Schema rỗng cho tools không cần arg (StructuredTool expects a model)."""
    pass
# Create StructuredTool with empty args schema
# get_categories = StructuredTool.from_function(
#     func=get_categories_func,
#     name="get_categories",
#     description="Lấy danh sách tất cả danh mục sản phẩm (không cần args).",
# )

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
        redis_client.setex(cache_key, 86400, context)
        return context
    except Exception as e:
        return f"Error getting product context: {str(e)}"

# ==================== DOCUMENT TOOLS ====================

def _serialize_docs_to_json(docs) -> str:
    """
    Tìm kiếm tài liệu liên quan đến câu hỏi trong vector database.
    Dùng để tìm thông tin về cửa hàng, chính sách, hướng dẫn.
    
    Args:
        query: Câu hỏi hoặc từ khóa cần tìm
        k: Số lượng tài liệu trả về (mặc định 5)
    
    Returns:
        JSON string chứa danh sách tài liệu liên quan
    """
    results = []
    for doc in docs:
        # doc may be different shapes depending on retriever, handle common attrs
        content = getattr(doc, "page_content", None) or getattr(doc, "content", None) or str(doc)
        metadata = getattr(doc, "metadata", None) or {}
        results.append({"content": content, "metadata": metadata})
    return json.dumps(results, ensure_ascii=False)

def search_documents_func(query: str, k: int = 5) -> str:
    """
    Tìm kiếm các tài liệu liên quan từ vector store dựa trên truy vấn người dùng ví dụ danh sách sản phẩm, chi tiết sản phẩm.

    Args:
        query (str): Chuỗi truy vấn của người dùng.
        k (int, optional): Số lượng tài liệu liên quan tối đa muốn lấy. Mặc định là 5.

    Returns:
        str: Kết quả tìm kiếm được serialize thành JSON. Nếu có lỗi xảy ra,
             trả về JSON chứa thông tin lỗi.

    Notes:
        - Hàm này sử dụng vector_store để truy xuất tài liệu.
        - Do các phiên bản của retriever khác nhau có thể sử dụng API khác nhau,
          hàm thử lần lượt các phương thức: get_relevant_documents(), retrieve(), invoke().
        - _serialize_docs_to_json(docs) là hàm riêng chịu trách nhiệm chuyển danh sách
          tài liệu thành JSON.
    """
    try:
        retriever = vector_store.as_retriever(search_kwargs={"k": k})
        # Different versions of retriever may expose different apis
        try:
            docs = retriever.get_relevant_documents(query)
        except AttributeError:
            try:
                docs = retriever.retrieve(query)
            except AttributeError:
                # fallback to invoke if present
                docs = retriever.invoke(query)

        return _serialize_docs_to_json(docs)
    except Exception as e:
        return json.dumps([{"error": str(e)}])

search_documents = StructuredTool.from_function(
    func=search_documents_func,
    name="search_documents",
    description="Tìm kiếm tài liệu liên quan trong vector DB. Args: query (str), k (int).",
    args_schema=SearchDocumentsInput
)

def add_document_to_knowledge_base_func(file_path: str, doc_type: str = "general", description: str = "") -> str:
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
        metadata = {"type": doc_type, "description": description}
        chunks = doc_processor.process_document(file_path, metadata)
        vector_store.add_documents(chunks)
        result = {
            "success": True,
            "message": f"Added {len(chunks)} chunks to knowledge base",
            "chunks_count": len(chunks)
        }
        return json.dumps(result, ensure_ascii=False)
    except Exception as e:
        return json.dumps({"success": False, "error": str(e)})

add_document_to_knowledge_base = StructuredTool.from_function(
    func=add_document_to_knowledge_base_func,
    name="add_document_to_knowledge_base",
    description="Thêm tài liệu vào knowledge base (file_path, doc_type, description).",
    args_schema=AddDocumentInput
)

# ==================== CART TOOLS (Structured for add, single for view) ====================

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
            # assume JSON response
            return response.json()
        else:
            return {"error": f"API returned {response.status_code}", "text": response.text}
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

def add_product_to_cart_func(product_id: int, quantity: int = 1, user_token: Optional[str] = None) -> str:
    """
    Thêm sản phẩm vào giỏ hàng.
    
    Args:
        product_id: ID sản phẩm cần thêm
        quantity: Số lượng (mặc định 1)
        user_token: JWT token của user (optional)
    
    Returns:
        JSON string với kết quả thêm vào giỏ hàng
    """
    try:
        json_data = {'ProductId': product_id, 'Quantity': quantity}
        result = call_csharp_cart_api('cart', method='POST', json_data=json_data, token=user_token if user_token else None)
        return json.dumps(result, ensure_ascii=False)
    except Exception as e:
        return json.dumps({"error": str(e)})

add_product_to_cart = StructuredTool.from_function(
    func=add_product_to_cart_func,
    name="add_product_to_cart",
    description="Thêm sản phẩm vào giỏ hàng. Args: product_id (int), quantity (int), user_token (str|optional).",
    args_schema=AddToCartInput
)

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
        product_keys = redis_client.keys("products:*") or []
        individual_keys = redis_client.keys("product:*") or []

        if product_keys:
            redis_client.delete(*product_keys)
        if individual_keys:
            redis_client.delete(*individual_keys)

        redis_client.delete("rag:product_context")
        redis_client.delete("categories:all")

        result = {
            "success": True,
            "message": f"Invalidated {len(product_keys) + len(individual_keys) + 2} cache keys"
        }
        return json.dumps(result, ensure_ascii=False)
    except Exception as e:
        return json.dumps({"success": False, "error": str(e)})

# ==================== EXPORT ALL TOOLS ====================

ALL_TOOLS = [
    # Product tools (StructuredTool objects or tool-decorated callables)
    # get_products,
    search_products,
    # get_product_details,
    get_categories,
    get_product_context_for_rag,

    # Document tools
    search_documents,
    add_document_to_knowledge_base,

    # Cart tools
    view_shopping_cart,
    add_product_to_cart,

    # Utility
    invalidate_product_cache,
]
