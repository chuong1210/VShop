"""
MCP Client với LangChain
Kết nối đến các MCP servers và sử dụng tools thông qua LangChain agent
"""
import asyncio
from typing import Dict, Any, List, Optional
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from langchain_mcp_adapters.tools import load_mcp_tools
from langgraph.prebuilt import create_react_agent
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain.prompts import PromptTemplate
from config.config import Config
import json
import os

class MCPClient:
    def __init__(self):
        """Khởi tạo MCP Client"""
        self.llm = ChatGoogleGenerativeAI(
            model="gemini-pro",
            google_api_key=Config.GOOGLE_API_KEY,
            temperature=0.2,
            convert_system_message_to_human=True  # 🟢 quan trọng

        )
        
        # Định nghĩa các MCP servers
        self.servers = [
            {
                "name": "products",
                "params": StdioServerParameters(
                    command="python",
                    args=[os.path.join("mcp_servers", "product_server.py")]
                )
            },
            {
                "name": "documents",
                "params": StdioServerParameters(
                    command="python",
                    args=[os.path.join("mcp_servers", "document_server.py")]
                )
            },
            {
                "name": "cart",
                "params": StdioServerParameters(
                    command="python",
                    args=[os.path.join("mcp_servers", "cart_server.py")]
                )
            }
        ]
        
        self.connections = []
        self.agent = None
        self.all_tools = []
    
    async def connect_to_server(self, server_config: Dict) -> Optional[Dict]:
        """Kết nối đến một MCP server và load tools"""
        name = server_config["name"]
        params = server_config["params"]
        
        try:
            read, write = await stdio_client(params).__aenter__()
            session = ClientSession(read, write)
            await session.__aenter__()
            await session.initialize()
            
            tools = await load_mcp_tools(session)
            
            print(f"✓ Connected to {name} server with {len(tools)} tools")
            
            return {
                "name": name,
                "session": session,
                "tools": tools,
                "client": stdio_client(params)
            }
        except Exception as e:
            print(f"❌ Error connecting to {name}: {e}")
            import traceback
            traceback.print_exc()
            return None
    
    async def initialize(self):
        """Khởi tạo kết nối đến tất cả MCP servers"""
        print("🔄 Connecting to MCP servers...")
        
        for server in self.servers:
            connection = await self.connect_to_server(server)
            if connection:
                self.connections.append(connection)
        
        # Tập hợp tất cả tools
        self.all_tools = []
        for connection in self.connections:
            self.all_tools.extend(connection["tools"])
        
        print(f"✓ Loaded {len(self.all_tools)} tools from {len(self.connections)} servers")
        
        # In danh sách tools
        print("\n📋 Available tools:")
        for tool in self.all_tools:
            print(f"  - {tool.name}: {tool.description[:80]}...")
        
        # Tạo agent với tất cả tools
        self.agent = create_react_agent(self.llm, self.all_tools)
        print("\n✓ RAG Agent initialized with MCP tools\n")
    
    def _detect_intent(self, query: str) -> Dict[str, Any]:
        """Phát hiện ý định của khách hàng"""
        query_lower = query.lower()
        
        # Intent: Hiển thị danh sách sản phẩm
        if any(keyword in query_lower for keyword in ['danh sách', 'xem sản phẩm', 'có những', 'liệt kê', 'show', 'hiển thị', 'các sản phẩm', 'list']):
            return {"action": "show_products"}
        
        # Intent: Gợi ý sản phẩm
        if any(keyword in query_lower for keyword in ['gợi ý', 'đề xuất', 'tư vấn', 'nên mua', 'recommend', 'giới thiệu']):
            return {"action": "get_recommendations"}
        
        # Intent: Tìm kiếm sản phẩm
        if any(keyword in query_lower for keyword in ['tìm', 'search', 'có', 'bán']):
            return {"action": "search_products"}
        
        # Intent: Giỏ hàng
        if any(keyword in query_lower for keyword in ['giỏ hàng', 'cart', 'đã thêm', 'trong giỏ']):
            return {"action": "view_cart"}
        
        return {"action": "chat"}
    
    async def chat(self, query: str, session_id: str = "default", user_token: str = None) -> Dict[str, Any]:
        """Xử lý chat với agent"""
        try:
            if not self.agent:
                await self.initialize()
            
            # Detect intent
            intent = self._detect_intent(query)
            
            # Tạo enhanced prompt với context
            system_prompt = f"""
Bạn là trợ lý mua sắm thông minh của VShop - một cửa hàng điện tử trực tuyến.

CÔNG CỤ CÓ SẴN:
Bạn có thể sử dụng các công cụ sau để giúp khách hàng:

1. QUẢN LÝ SẢN PHẨM:
   - get_products: Lấy danh sách sản phẩm (có thể lọc theo category_id)
   - search_products: Tìm kiếm sản phẩm theo từ khóa
   - get_product_details: Xem chi tiết một sản phẩm cụ thể
   - get_categories: Lấy danh sách danh mục
   - get_products_context: Lấy thông tin tổng quan về sản phẩm

2. TÌM KIẾM TÀI LIỆU:
   - search_documents: Tìm kiếm thông tin trong tài liệu cửa hàng (chính sách, hướng dẫn)

3. QUẢN LÝ GIỎ HÀNG:
   - add_to_cart: Thêm sản phẩm vào giỏ hàng
   - get_cart: Xem giỏ hàng hiện tại
   - remove_from_cart: Xóa sản phẩm khỏi giỏ
   - update_cart_quantity: Cập nhật số lượng
   - clear_cart: Xóa toàn bộ giỏ hàng

HƯỚNG DẪN:
- Session ID: {session_id}
- User Token: {'Available' if user_token else 'Not available'}
- Intent phát hiện: {intent['action']}

- Khi khách hỏi về sản phẩm, PHẢI dùng get_products hoặc search_products
- Khi khách muốn thêm vào giỏ, PHẢI dùng add_to_cart với product_id và token (nếu có)
- Khi khách hỏi về cửa hàng/chính sách, dùng search_documents
- Luôn trả lời thân thiện, chuyên nghiệp
- Nếu cần thông tin sản phẩm, GỌI TOOL trước khi trả lời
- Giá cả phải chính xác từ database

CÂU HỎI KHÁCH HÀNG: {query}
"""
            
            # Gọi agent với enhanced prompt
            print(f"\n💬 Processing query: {query[:50]}...")
            response = await self.agent.ainvoke({"messages": system_prompt})
            
            # Parse response
            messages = response.get("messages", [])
            if not messages:
                return {
                    "answer": "Xin lỗi, tôi không thể xử lý yêu cầu này.",
                    "action": "error"
                }
            
            last_message = messages[-1]
            answer = last_message.content if hasattr(last_message, 'content') else str(last_message)
            
            # Trích xuất tool calls và kết quả
            tool_calls = []
            tool_results = []
            
            for msg in messages:
                if hasattr(msg, 'tool_calls') and msg.tool_calls:
                    for tc in msg.tool_calls:
                        tool_calls.append({
                            'name': tc.get('name', 'unknown'),
                            'args': tc.get('args', {})
                        })
                
                # Kiểm tra nếu message chứa tool result
                if hasattr(msg, 'content') and isinstance(msg.content, (list, dict)):
                    tool_results.append(msg.content)
            
            # Kiểm tra xem có products trong tool results không
            products = []
            for result in tool_results:
                if isinstance(result, list):
                    # Có thể là danh sách products
                    for item in result:
                        if isinstance(item, dict) and 'Name' in item and 'Price' in item:
                            products.append(item)
                elif isinstance(result, dict) and 'Name' in result and 'Price' in result:
                    products.append(result)
            
            response_data = {
                "answer": answer,
                "action": intent['action'],
                "tool_calls": tool_calls,
            }
            
            # Nếu có products, thêm vào response
            if products:
                response_data["products"] = products
                response_data["action"] = "show_products"
            
            print(f"✓ Response generated with {len(tool_calls)} tool calls")
            
            return response_data
            
        except Exception as e:
            print(f"❌ Error in chat: {e}")
            import traceback
            traceback.print_exc()
            return {
                "answer": "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.",
                "action": "error",
                "error": str(e)
            }
    
    async def get_products_direct(self, category_id: Optional[int] = None, limit: int = 10) -> List[Dict]:
        """Gọi trực tiếp tool get_products (không qua agent)"""
        try:
            # Tìm tool get_products
            get_products_tool = next((t for t in self.all_tools if t.name == "get_products"), None)
            
            if not get_products_tool:
                return []
            
            # Gọi tool
            result = await get_products_tool.ainvoke({
                "category_id": category_id,
                "limit": limit
            })
            
            return result if isinstance(result, list) else []
            
        except Exception as e:
            print(f"Error getting products: {e}")
            return []
    
    async def search_products_direct(self, search_term: str, limit: int = 10) -> List[Dict]:
        """Gọi trực tiếp tool search_products"""
        try:
            search_tool = next((t for t in self.all_tools if t.name == "search_products"), None)
            
            if not search_tool:
                return []
            
            result = await search_tool.ainvoke({
                "search_term": search_term,
                "limit": limit
            })
            
            return result if isinstance(result, list) else []
            
        except Exception as e:
            print(f"Error searching products: {e}")
            return []
    
    async def cleanup(self):
        """Đóng tất cả kết nối"""
        print("\n🔄 Closing MCP connections...")
        for connection in self.connections:
            try:
                await connection["session"].__aexit__(None, None, None)
                await connection["client"].__aexit__(None, None, None)
            except Exception as e:
                print(f"Error closing connection: {e}")
        print("✓ All MCP connections closed")

# Singleton instance
_mcp_client = None
_mcp_client_lock = asyncio.Lock()

async def get_mcp_client() -> MCPClient:
    """Get or create MCP client instance (singleton)"""
    global _mcp_client
    
    async with _mcp_client_lock:
        if _mcp_client is None:
            _mcp_client = MCPClient()
            await _mcp_client.initialize()
    
    return _mcp_client