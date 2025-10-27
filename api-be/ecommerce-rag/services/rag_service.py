"""
RAG Service với LangChain Agent và Tools
"""
from typing import Dict, Any
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain.prompts import ChatPromptTemplate, HumanMessagePromptTemplate, PromptTemplate, MessagesPlaceholder
from langchain.agents import create_react_agent, AgentExecutor  # ← Thay structured bằng react
from langchain.memory import ConversationBufferMemory
from config.config import Config
from services.tools import ALL_TOOLS
from services.db_service import DatabaseService
import json
from langchain import hub  # Để pull ReAct prompt chuẩn
from langchain_core.messages import AIMessage, HumanMessage

# Parse JSON output và giới thiệu 
class RAGService:
    def __init__(self):
        # Initialize LLM
        self.llm = ChatGoogleGenerativeAI(
            model="gemini-2.0-flash",
            google_api_key=Config.GOOGLE_API_KEY,
            temperature=0.2,
            convert_system_message_to_human=True , # 🟢 quan trọng
            max_output_tokens=2000  # giới hạn output ~200 tokens (~150-180 từ)

            ,streaming=False
        )

        # Initialize database service
        self.db_service = DatabaseService()
        self.system_prompt = """Bạn là trợ lý mua sắm thông minh của VShop - cửa hàng điện tử trực tuyến.

        NHIỆM VỤ:
        1. Tư vấn và giới thiệu sản phẩm cho khách hàng
        2. Trả lời câu hỏi về cửa hàng, chính sách, dịch vụ
        3. Hỗ trợ khách hàng mua sắm và quản lý giỏ hàng

        HƯỚNG DẪN TOOL CALLING (PHẢI TUÂN THEO):
        - Nếu khách TÌM KIẾM SẢN PHẨM (ví dụ: 'tìm tai nghe', 'sản phẩm có giá dưới 1tr'), PHẢI gọi tool 'search_products' với search_term=query và limit=10. TRẢ VỀ JSON RAW PRODUCTS..
        - Nếu hỏi GIỎ HÀNG, gọi 'view_shopping_cart'.
        - Nếu hỏi CHÍNH SÁCH/CỬA HÀNG/thông tin sản phẩm/ Thông tin cửa hàng, gọi 'search_documents'.
        - Luôn parse JSON từ tool output để lấy dữ liệu chính xác (giá, tên, mô tả). Trả lời THÂN THIỆN, CHUYÊN NGHIỆP bằng TIẾNG VIỆT. Không hallucinate dữ liệu.
        - Nếu không cần tool, chat tự nhiên. Giới hạn output ngắn gọn.

        Sử dụng các tool sau khi cần: {tools} (tên: {tool_names})."""

        self.user_prompt = "{input}"  # Đơn giản hóa, không cần "Người dùng hỏi:"
        

        # Initialize Agent với tất cả tools
        self.agent = self._create_agent()

        print(f"✓ RAG Agent initialized with {len(ALL_TOOLS)} tools")
        self._print_available_tools()

    def _print_available_tools(self):
        """In danh sách tools có sẵn"""
        print("\n📋 Available Tools:")
        for tool in ALL_TOOLS:
            print(f"  - {tool.name}: {tool.description[:80]}...")
        print()
    def _create_agent(self) -> AgentExecutor:
        """Tạo ReAct Agent với prompt chuẩn từ Hub"""
        # Memory giữ nguyên
        memory = ConversationBufferMemory(
            memory_key="chat_history",
            return_messages=True,
            output_key="output"
        )

        # Pull ReAct prompt chuẩn từ Hub (đã format {tools}, {tool_names}, {input}, {agent_scratchpad})
        react_prompt = hub.pull("hwchase17/react")  # Standard English ReAct

        # Localize: Prepend system_prompt (hướng dẫn tool bằng VN) vào đầu react_prompt
        # Đảm bảo không có JSON malformed – dùng f-string an toàn
        system_guidance = self.system_prompt.replace("{tools}", "{tools}").replace("{tool_names}", "{tool_names}")  # Giữ placeholders
        full_prompt = system_guidance + "\n\n" + react_prompt.template  # Kết hợp

        # Tạo PromptTemplate
        prompt = PromptTemplate.from_template(full_prompt)

        # Tạo agent
        agent = create_react_agent(
            llm=self.llm,
            tools=ALL_TOOLS,
            prompt=prompt
        )

        # Executor (giữ fix early_stopping)
        agent_executor = AgentExecutor(
            agent=agent,
            tools=ALL_TOOLS,
            verbose=True,
            memory=memory,
            handle_parsing_errors=True,
            return_intermediate_steps=True,
            # max_iterations=3,
            early_stopping_method="force"  # An toàn
        )

        return agent_executor
    # def _create_agent(self) -> AgentExecutor:
    #     """Tạo LangChain Agent tương thích StructuredTool"""

    #     # Tạo memory cho conversation
    #     memory = ConversationBufferMemory(
    #         memory_key="chat_history",
    #         return_messages=True,
    #         output_key="output"
    #     )

    #     # Prompt chuẩn cho structured chat agent
    #     prompt = ChatPromptTemplate.from_messages([
    #         ("system", "Bạn là trợ lý mua sắm thông minh của VShop. Hãy dùng tools khi cần."),
    #         MessagesPlaceholder(variable_name="chat_history"),
    #         ("user", "{input}"),
    #         MessagesPlaceholder(variable_name="agent_scratchpad"),
    #     ])
    #     messages = [
    #         ("system", self.system_prompt),
    #         ("placeholder", "{chat_history}"),
    #         HumanMessagePromptTemplate(
    #             prompt=PromptTemplate(input_variables=["input"], template=self.user_prompt)
    #         ),
    #         ("ai", "{agent_scratchpad}"),  
    #     ]
        
    #     prompt = ChatPromptTemplate.from_messages(
    #    messages

    # )


    #     # Tạo structured agent
    #     agent = create_structured_chat_agent(
    #         llm=self.llm,
    #         tools=ALL_TOOLS,
        
    #         prompt=prompt
    #     )

    #     # Tạo AgentExecutor (kết hợp agent + tools + memory)
    #     agent_executor = AgentExecutor.from_agent_and_tools(
    #         agent=agent,
    #         tools=ALL_TOOLS,
    #         verbose=True,
    #         memory=memory,
    #         handle_parsing_errors=True,
    #         return_intermediate_steps=True,
    #         max_iterations=5,
    #         agent_kwargs={"handle_parsing_errors": True}
    #     )
        

    #     return agent_executor
    

    def format_log_to_messages(self,intermediate_steps):
        """Construct the scratchpad that lets the agent continue its thought process."""
        thoughts = []
        for action, observation in intermediate_steps:
            thoughts.append(AIMessage(content=action.log))
            human_message = HumanMessage(content=f"Observation: {observation}")
            thoughts.append(human_message)
        return thoughts

    # Ensure agent_scratchpad is formatted correctly

    def _detect_intent(self, query: str) -> Dict[str, Any]:
        """Phát hiện ý định của khách hàng"""
        query_lower = query.lower()

        if any(k in query_lower for k in ['danh sách', 'xem sản phẩm', 'liệt kê', 'show', 'list']):
            return {"action": "show_products"}
        if any(k in query_lower for k in ['tìm', 'search', 'có', 'bán']):
            return {"action": "search_products"}
        if any(k in query_lower for k in ['giỏ hàng', 'cart']):
            return {"action": "view_cart"}

        return {"action": "chat"}
    def chat(self, query: str, session_id: str = "default", user_token: str = None) -> Dict[str, Any]:
        try:
            intent = self._detect_intent(query)
            print(f"🔍 Detected intent: {intent['action']} for query: '{query}'")

            # Invoke agent
            response_dict = self.agent.invoke({
                "input": query
            })
            response = response_dict.get("output", "")

            # Log steps
            intermediate_steps = response_dict.get("intermediate_steps", [])
            if intermediate_steps:
                print(f"🛠️ Tool calls: {len(intermediate_steps)}")
                for action, obs in intermediate_steps:
                    print(f"  - Tool: {action.tool} | Input: {action.tool_input}")
                    print(f"    Obs: {obs[:100]}...")

            # Parse products (từ obs nếu response không có)
            products = None
            for _, obs in intermediate_steps:
                try:
                    import json, re
                    json_match = re.search(r'\[.*?\]|\{.*?\}', obs, re.DOTALL)
                    if json_match:
                        parsed = json.loads(json_match.group())
                        if isinstance(parsed, list) and len(parsed) > 0 and isinstance(parsed[0], dict):
                            products = parsed
                            break
                except:
                    pass

            result = {
                "answer": response,
                "action": intent['action']
            }
            if products:
                result["products"] = products
                result["action"] = "show_products"

            self.agent.memory.save_context({"input": query}, {"output": response})
            return result

        except ValueError as ve:
            # Fallback cho early_stopping error (nếu vẫn xảy ra)
            print(f"⚠️ Agent ValueError fallback: {ve}")
            if "early_stopping_method" in str(ve):
                # Gọi tool thủ công cho intent rõ (e.g., show_products)
                if intent['action'] == 'show_products':
                    from services.tools import get_products  # Import tool
                    tool_output = get_products.invoke({"category_id": None, "limit": 10})
                    import json
                    products = json.loads(tool_output)
                    response = f"Dưới đây là danh sách 10 sản phẩm hot tại VShop:\n{json.dumps(products, ensure_ascii=False, indent=2)}"
                    return {"answer": response, "action": "show_products", "products": products}
            # Re-raise nếu không phải early_stopping
            raise

        except Exception as e:
            print(f"❌ Error: {e}")
            import traceback
            traceback.print_exc()
            return {"answer": "Xin lỗi, lỗi hệ thống. Thử lại nhé!", "action": "error", "error": str(e)}
    def get_product_details(self, product_id: int) -> Dict[str, Any]:
        """Lấy thông tin chi tiết sản phẩm"""
        return self.db_service.get_product_by_id(product_id)
