"""
MCP Server cho quản lý sản phẩm
Cung cấp các công cụ: get_products, search_products, get_product_details
"""
from mcp.server.fastmcp import FastMCP
from typing import List, Dict, Optional
import sys
import os
import redis
import json

# Add parent directory to path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from services.db_service import DatabaseService
from config.config import Config

# Khởi tạo MCP Server
mcp = FastMCP("ProductServer")

# Khởi tạo Database Service
db_service = DatabaseService()

# Redis client cho caching
redis_client = redis.StrictRedis(
    host=Config.REDIS_HOST, 
    port=Config.REDIS_PORT, 
    db=Config.REDIS_DB, 
    decode_responses=True
)

@mcp.tool()
def get_products(category_id: Optional[int] = None, limit: int = 10) -> List[Dict]:
    """
    Lấy danh sách sản phẩm với caching
    
    Args:
        category_id: ID danh mục (optional)
        limit: Số lượng sản phẩm tối đa
    
    Returns:
        List[Dict]: Danh sách sản phẩm
    """
    try:
        # Tạo cache key
        cache_key = f"products:category:{category_id}:limit:{limit}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"Cache HIT: {cache_key}")
            return json.loads(cached)
        
        # Cache MISS - query database
        print(f"Cache MISS: {cache_key}")
        products = db_service.get_products(category_id=category_id, limit=limit)
        
        # Lưu vào cache (TTL 5 phút)
        redis_client.setex(cache_key, 300, json.dumps(products))
        
        return products
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def search_products(search_term: str, limit: int = 10) -> List[Dict]:
    """
    Tìm kiếm sản phẩm theo từ khóa với caching
    
    Args:
        search_term: Từ khóa tìm kiếm
        limit: Số lượng sản phẩm tối đa
    
    Returns:
        List[Dict]: Danh sách sản phẩm tìm được
    """
    try:
        # Tạo cache key
        cache_key = f"products:search:{search_term}:limit:{limit}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"Cache HIT: {cache_key}")
            return json.loads(cached)
        
        # Cache MISS
        print(f"Cache MISS: {cache_key}")
        products = db_service.get_products(search_term=search_term, limit=limit)
        
        # Lưu vào cache (TTL 5 phút)
        redis_client.setex(cache_key, 300, json.dumps(products))
        
        return products
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def get_product_details(product_id: int) -> Dict:
    """
    Lấy thông tin chi tiết của một sản phẩm với caching
    
    Args:
        product_id: ID sản phẩm
    
    Returns:
        Dict: Thông tin chi tiết sản phẩm
    """
    try:
        # Tạo cache key
        cache_key = f"product:{product_id}"
        
        # Kiểm tra cache
        cached = redis_client.get(cache_key)
        if cached:
            print(f"Cache HIT: {cache_key}")
            return json.loads(cached)
        
        # Cache MISS
        print(f"Cache MISS: {cache_key}")
        product = db_service.get_product_by_id(product_id)
        
        if product:
            # Lưu vào cache (TTL 10 phút)
            redis_client.setex(cache_key, 600, json.dumps(product))
            return product
        else:
            return {"error": "Product not found"}
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def get_categories() -> List[Dict]:
    """
    Lấy danh sách tất cả danh mục sản phẩm với caching
    
    Returns:
        List[Dict]: Danh sách danh mục
    """
    try:
        cache_key = "categories:all"
        
        cached = redis_client.get(cache_key)
        if cached:
            print(f"Cache HIT: {cache_key}")
            return json.loads(cached)
        
        print(f"Cache MISS: {cache_key}")
        categories = db_service.get_categories()
        
        # Lưu vào cache (TTL 1 giờ - categories ít thay đổi)
        redis_client.setex(cache_key, 3600, json.dumps(categories))
        
        return categories
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def get_products_context() -> str:
    """
    Lấy thông tin sản phẩm dưới dạng text context cho RAG với caching
    
    Returns:
        str: Text context về sản phẩm
    """
    try:
        cache_key = "rag:product_context"
        
        cached = redis_client.get(cache_key)
        if cached:
            print(f"Cache HIT: {cache_key}")
            return cached
        
        print(f"Cache MISS: {cache_key}")
        context = db_service.get_products_for_rag_context()
        
        # Lưu vào cache (TTL 1 ngày)
        redis_client.setex(cache_key, 86400, context)
        
        return context
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def invalidate_product_cache() -> Dict:
    """
    Xóa toàn bộ cache liên quan đến sản phẩm
    (Được gọi khi có event từ Kafka)
    
    Returns:
        Dict: Kết quả invalidation
    """
    try:
        # Xóa tất cả keys liên quan đến products
        pattern = "products:*"
        keys = redis_client.keys(pattern)
        if keys:
            redis_client.delete(*keys)
        
        # Xóa product context cache
        redis_client.delete("rag:product_context")
        
        # Xóa individual product caches
        product_keys = redis_client.keys("product:*")
        if product_keys:
            redis_client.delete(*product_keys)
        
        return {
            "success": True,
            "message": f"Invalidated {len(keys) + len(product_keys) + 1} cache keys"
        }
    except Exception as e:
        return {"success": False, "error": str(e)}

if __name__ == "__main__":
    # Chạy MCP server với stdio transport
    mcp.run(transport="stdio")