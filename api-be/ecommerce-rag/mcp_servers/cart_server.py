"""
MCP Server cho quản lý giỏ hàng
Gọi C# API thay vì lưu local
"""
from mcp.server.fastmcp import FastMCP
from typing import Dict
import sys
import os
import requests

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from config.config import Config

# Khởi tạo MCP Server
mcp = FastMCP("CartServer")

def call_csharp_cart_api(endpoint: str, method: str = 'GET', json_data: dict = None, token: str = None) -> Dict:
    """Helper function để gọi C# Cart API"""
    try:
        headers = {
            'Content-Type': 'application/json'
        }
        
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
            return {"error": f"API returned {response.status_code}", "details": response.text}
            
    except requests.exceptions.Timeout:
        return {"error": "Request timeout"}
    except requests.exceptions.RequestException as e:
        return {"error": f"Request failed: {str(e)}"}
    except Exception as e:
        return {"error": str(e)}

@mcp.tool()
def add_to_cart(product_id: int, quantity: int = 1, token: str = None) -> Dict:
    """
    Thêm sản phẩm vào giỏ hàng qua C# API
    
    Args:
        product_id: ID sản phẩm
        quantity: Số lượng sản phẩm
        token: JWT token (optional)
    
    Returns:
        Dict: Kết quả từ C# API
    """
    json_data = {
        'ProductId': product_id,
        'Quantity': quantity
    }
    
    result = call_csharp_cart_api('cart', method='POST', json_data=json_data, token=token)
    return result

@mcp.tool()
def get_cart(token: str = None) -> Dict:
    """
    Lấy thông tin giỏ hàng từ C# API
    
    Args:
        token: JWT token (optional)
    
    Returns:
        Dict: Thông tin giỏ hàng
    """
    result = call_csharp_cart_api('cart', method='GET', token=token)
    return result

@mcp.tool()
def remove_from_cart(product_id: int, token: str = None) -> Dict:
    """
    Xóa sản phẩm khỏi giỏ hàng qua C# API
    
    Args:
        product_id: ID sản phẩm cần xóa
        token: JWT token (optional)
    
    Returns:
        Dict: Kết quả từ C# API
    """
    result = call_csharp_cart_api(f'cart-remove?ProductId={product_id}', method='POST', token=token)
    return result

@mcp.tool()
def update_cart_quantity(product_id: int, quantity: int, token: str = None) -> Dict:
    """
    Cập nhật số lượng sản phẩm trong giỏ hàng qua C# API
    
    Args:
        product_id: ID sản phẩm
        quantity: Số lượng mới
        token: JWT token (optional)
    
    Returns:
        Dict: Kết quả từ C# API
    """
    json_data = {
        'ProductId': product_id,
        'Quantity': quantity
    }
    
    result = call_csharp_cart_api('cart', method='PUT', json_data=json_data, token=token)
    return result

@mcp.tool()
def clear_cart(token: str = None) -> Dict:
    """
    Xóa toàn bộ giỏ hàng qua C# API
    
    Args:
        token: JWT token (optional)
    
    Returns:
        Dict: Kết quả từ C# API
    """
    result = call_csharp_cart_api('cart/clear', method='POST', token=token)
    return result

if __name__ == "__main__":
    mcp.run(transport="stdio")