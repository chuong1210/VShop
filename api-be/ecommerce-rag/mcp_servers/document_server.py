"""
MCP Server cho quản lý tài liệu
Cung cấp các công cụ: search_documents, add_document
"""
from mcp.server.fastmcp import FastMCP
from typing import List, Dict
import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from langchain_elasticsearch import ElasticsearchStore
from utils.embeddings import LocalEmbeddings
from utils.document_processor import DocumentProcessor
from config.config import Config

# Khởi tạo MCP Server
mcp = FastMCP("DocumentServer")

# Khởi tạo Embeddings và Vector Store
embeddings = LocalEmbeddings(model_name="paraphrase-multilingual-MiniLM-L12-v2")
vector_store = ElasticsearchStore(
    es_url=Config.ELASTICSEARCH_URL,
    index_name=Config.ES_INDEX_NAME,
    embedding=embeddings
)
doc_processor = DocumentProcessor()

@mcp.tool()
def search_documents(query: str, k: int = 5) -> List[Dict]:
    """
    Tìm kiếm tài liệu liên quan đến câu hỏi
    
    Args:
        query: Câu hỏi hoặc từ khóa tìm kiếm
        k: Số lượng tài liệu trả về
    
    Returns:
        List[Dict]: Danh sách tài liệu liên quan
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
        
        return results
    except Exception as e:
        return [{"error": str(e)}]

@mcp.tool()
def add_document(file_path: str, doc_type: str = "general", description: str = "") -> Dict:
    """
    Thêm tài liệu mới vào hệ thống RAG
    
    Args:
        file_path: Đường dẫn đến file tài liệu
        doc_type: Loại tài liệu (general, policy, guide, product_info)
        description: Mô tả tài liệu
    
    Returns:
        Dict: Kết quả thêm tài liệu
    """
    try:
        metadata = {
            "type": doc_type,
            "description": description
        }
        
        chunks = doc_processor.process_document(file_path, metadata)
        vector_store.add_documents(chunks)
        
        return {
            "success": True,
            "message": f"Added {len(chunks)} chunks to vector store",
            "chunks_count": len(chunks)
        }
    except Exception as e:
        return {
            "success": False,
            "error": str(e)
        }

if __name__ == "__main__":
    mcp.run(transport="stdio")