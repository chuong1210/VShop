from sentence_transformers import SentenceTransformer
from typing import List
import numpy as np

class LocalEmbeddings:
    """Local embeddings using Sentence Transformers (free, no API calls)"""
    
    def __init__(self, model_name: str = "paraphrase-multilingual-MiniLM-L12-v2"):
        """
        Initialize local embedding model
        
        Args:
            model_name: Model name from sentence-transformers
                - paraphrase-multilingual-MiniLM-L12-v2: 384 dim, supports Vietnamese
                - all-MiniLM-L6-v2: 384 dim, English only, faster
                - paraphrase-multilingual-mpnet-base-v2: 768 dim, better quality
        """
        print(f"Loading embedding model: {model_name}")
        self.model = SentenceTransformer(model_name)
        print(f"Model loaded successfully! Dimension: {self.model.get_sentence_embedding_dimension()}")
    
    def embed_documents(self, texts: List[str]) -> List[List[float]]:
        """Embed multiple documents"""
        embeddings = self.model.encode(texts, convert_to_numpy=True)
        return embeddings.tolist()
    
    def embed_query(self, text: str) -> List[float]:
        """Embed a single query"""
        embedding = self.model.encode(text, convert_to_numpy=True)
        return embedding.tolist()
    
    def get_dimension(self) -> int:
        """Get embedding dimension"""
        return self.model.get_sentence_embedding_dimension()