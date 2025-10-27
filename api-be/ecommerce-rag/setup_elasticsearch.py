from elasticsearch import Elasticsearch
from config.config import Config

def setup_elasticsearch():
    """Khởi tạo index Elasticsearch với dimension 384 cho sentence-transformers"""
    try:
        es = Elasticsearch([Config.ELASTICSEARCH_URL])
        
        # Delete existing index if exists
        if es.indices.exists(index=Config.ES_INDEX_NAME):
            print(f"Deleting existing index {Config.ES_INDEX_NAME}")
            es.indices.delete(index=Config.ES_INDEX_NAME)
        
        # Create index with correct mapping for Sentence Transformers (384 dimensions)
        mapping = {
            "mappings": {
                "properties": {
                    "text": {
                        "type": "text",
                        "analyzer": "standard"
                    },
                    "vector": {
                        "type": "dense_vector",
                        "dims": 384,  # Dimension của paraphrase-multilingual-MiniLM-L12-v2
                        "index": True,
                        "similarity": "cosine"
                    },
                    "metadata": {
                        "type": "object",
                        "enabled": True
                    }
                }
            },
            "settings": {
                "number_of_shards": 1,
                "number_of_replicas": 0,
                "index": {
                    "max_result_window": 10000
                }
            }
        }
        
        es.indices.create(index=Config.ES_INDEX_NAME, body=mapping)
        print(f"✓ Created Elasticsearch index '{Config.ES_INDEX_NAME}' with dimension 384")
        
    except Exception as e:
        print(f"✗ Error setting up Elasticsearch: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    setup_elasticsearch()