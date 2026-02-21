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
from langchain.agents import AgentOutputParser
from langchain.schema import AgentAction, AgentFinish
import re, json
from langchain_openai import ChatOpenAI # Dùng ChatOpenAI của LangChain để tích hợp

class SafeReActParser(AgentOutputParser):
    def parse(self, text: str):
        # Nếu model không xuất ra Action: => Trả về Final Answer
        if "Action:" not in text:
            # --- XỬ LÝ LÀM SẠCH OUTPUT TẠI ĐÂY ---
            cleaned_output = text.strip()
            
            # 1. Nếu có từ khóa "Final Answer:", chỉ lấy phần sau nó
            if "Final Answer:" in cleaned_output:
                cleaned_output = cleaned_output.split("Final Answer:")[-1].strip()
            
            # 2. Nếu model quên "Final Answer" mà vẫn dính "Thought:", cắt bỏ phần Thought đi
            elif "Thought:" in cleaned_output:
                cleaned_output = cleaned_output.split("Thought:")[-1].strip()
            # -------------------------------------

            return AgentFinish(
                return_values={"output": cleaned_output},
                log=text,
            )

        # --- PHẦN DƯỚI GIỮ NGUYÊN LOGIC CŨ CỦA BẠN ---
        # Trích Thought / Action / Action Input
        action_match = re.search(r"Action\s*:\s*(.*)", text)
        input_match = re.search(r"Action Input\s*:\s*(.*)", text)

        if action_match:
            action = action_match.group(1).strip()
            action_input = input_match.group(1).strip() if input_match else ""
            try:
                # Cố gắng parse JSON, nếu lỗi thì giữ nguyên string
                action_input = json.loads(action_input)
            except:
                pass
            return AgentAction(tool=action, tool_input=action_input, log=text)

        # fallback
        return AgentFinish(return_values={"output": text.strip()}, log=text)

def safe_parse_output(output):
    """Xử lý lỗi parser 'Missing Action' hoặc lỗi format."""
    if "Action:" not in output:
        return {"action": "Final Answer", "action_input": output}
    try:
        action_part = output.split("Action:")[1].strip()
        if "Action Input:" in action_part:
            name, action_input = action_part.split("Action Input:")
            return {"action": name.strip(), "action_input": action_input.strip()}
        return {"action": action_part.strip(), "action_input": ""}
    except Exception as e:
        return {"action": "Final Answer", "action_input": str(output)}
# Parse JSON output và giới thiệu 
class RAGService:
    def __init__(self):
        # Initialize LLM
        self.llm = ChatOpenAI(
            model="x-ai/grok-3-mini-beta",  # Tên model Grok trên OpenRouter
            openai_api_key="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",  # API Key của bạn từ OpenRouter
            openai_api_base="https://openrouter.ai/api/v1", # Base URL của OpenRouter
            temperature=0.2,
            max_tokens=2000,
            default_headers={ # Thêm các header tùy chọn như bài viết hướng dẫn
                "HTTP-Referer": "http://localhost:3000", # Thay bằng URL app của bạn
                "X-Title": "VShop RAG Assistant", # Tên app của bạn
            },
            stop=["\nObservation:", "Observation"],
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
            prompt=prompt,
            output_parser=SafeReActParser()


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

            # 1. Gọi Agent
            response_dict = self.agent.invoke({
                "input": query
            })
            
            # Lấy câu trả lời text
            response_text = response_dict.get("output", "")
            
            # Lấy các bước trung gian (Tool calls)
            intermediate_steps = response_dict.get("intermediate_steps", [])

            # Log để debug
            if intermediate_steps:
                print(f"🛠️ Tool calls: {len(intermediate_steps)}")

            # 2. TRÍCH XUẤT DỮ LIỆU SẢN PHẨM TỪ TOOL (QUAN TRỌNG NHẤT)
            products = None
            
            # Duyệt ngược từ cuối lên để lấy kết quả mới nhất
            for action_obj, obs in reversed(intermediate_steps):
                # Chỉ quan tâm đến tool tìm kiếm sản phẩm
                if action_obj.tool in ["search_products", "get_products", "get_recommendations"]:
                    try:
                        import json, re
                        
                        # Tìm chuỗi JSON dạng list [...]
                        json_match = re.search(r'\[.*\]', obs, re.DOTALL)
                        if json_match:
                            parsed = json.loads(json_match.group())
                            
                            # Kiểm tra kỹ xem có phải danh sách sản phẩm không
                            if isinstance(parsed, list) and len(parsed) > 0 and isinstance(parsed[0], dict):
                                # Lấy keys của phần tử đầu tiên, chuyển về chữ thường
                                keys = [k.lower() for k in parsed[0].keys()]
                                
                                # Chấp nhận nếu có 'name' hoặc 'price' hoặc 'id'
                                if any(k in keys for k in ['name', 'price', 'id', 'internal_code']):
                                    products = parsed
                                    print(f"✅ Extracted {len(products)} products from tool output.")
                                    break # Tìm thấy rồi thì dừng
                    except Exception as e:
                        print(f"⚠️ Error parsing tool output: {e}")
                        continue

            # 3. XỬ LÝ KẾT QUẢ TRẢ VỀ
            
            # Nếu AI trả lời kiểu "Không cần thêm..." nhưng ta ĐÃ tìm thấy products từ Tool
            # => Ta VẪN trả về products để Frontend hiển thị
            if products:
                # Nếu câu trả lời của AI quá vô dụng, ta tự viết lại câu trả lời
                if len(response_text) < 20 or "không cần" in response_text.lower():
                    response_text = f"Dưới đây là {len(products)} sản phẩm tôi tìm thấy theo yêu cầu của bạn:"
                
                return {
                    "answer": response_text,
                    "action": "show_products", # BẮT BUỘC action này
                    "products": products
                }

            # Trường hợp bình thường (Chat hoặc không tìm thấy sản phẩm)
            return {
                "answer": response_text,
                "action": intent['action']
            }

        except ValueError as ve:
            print(f"⚠️ Agent ValueError fallback: {ve}")
            if "early_stopping_method" in str(ve):
                # Fallback khẩn cấp nếu Agent bị ngắt
                if intent['action'] == 'search_products':
                     return {"answer": "Tôi đã tìm thấy sản phẩm nhưng bị ngắt quãng. Hãy thử lại.", "action": "chat"}
            raise

        except Exception as e:
            print(f"❌ Error: {e}")
            import traceback
            traceback.print_exc()
            return {"answer": "Xin lỗi, lỗi hệ thống. Thử lại nhé!", "action": "error", "error": str(e)}
    def get_product_details(self, product_id: int) -> Dict[str, Any]:
        """Lấy thông tin chi tiết sản phẩm"""
        return self.db_service.get_product_by_id(product_id)
