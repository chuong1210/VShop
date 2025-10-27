import os
from dotenv import load_dotenv

load_dotenv()

class Config:
    # Google API (chỉ dùng cho LLM, không dùng embeddings)
    GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY")
    
    # SQL Server
    SQL_SERVER_CONN = os.getenv("SQL_SERVER_CONN")
    
    # Elasticsearch
    ELASTICSEARCH_URL = os.getenv("ELASTICSEARCH_URL", "http://localhost:9200")
    ES_INDEX_NAME = "ecommerce_documents"
    
    # Redis
    REDIS_HOST = os.getenv("REDIS_HOST", "localhost")
    REDIS_PORT = int(os.getenv("REDIS_PORT", 6379))
    REDIS_DB = int(os.getenv("REDIS_DB", 0))
    
    # Kafka
    KAFKA_BROKER = os.getenv("KAFKA_BROKER", "localhost:9092")
    KAFKA_TOPIC = os.getenv("KAFKA_TOPIC", "product-changes")
    KAFKA_GROUP_ID = os.getenv("KAFKA_GROUP_ID", "rag-group")
    
    # C# API
    CSHARP_API_BASE = os.getenv("CSHARP_API_BASE", "https://localhost:7288/smw-api")
    
    # Flask
    SECRET_KEY = os.getenv("FLASK_SECRET_KEY", "dev-secret-key")
    UPLOAD_FOLDER = "uploads"
    MAX_CONTENT_LENGTH = 16 * 1024 * 1024  # 16MB max file size
    
    # RAG Settings
    CHUNK_SIZE = 800
    CHUNK_OVERLAP = 100
    TOP_K_RESULTS = 5
    
    # Recommendation API
    RECOMMENDATION_API_URL = os.getenv("RECOMMENDATION_API_URL", "http://localhost:5001/api/recommend")