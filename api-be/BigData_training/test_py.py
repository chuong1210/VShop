# test_run.py
import sys
print("Python path:", sys.executable)

try:
    from flask import Flask
    print("✓ Flask imported")
except Exception as e:
    print("✗ Flask error:", e)

try:
    from elasticsearch import Elasticsearch
    print("✓ Elasticsearch imported")
except Exception as e:
    print("✗ Elasticsearch error:", e)

try:
    from sqlalchemy import create_engine
    print("✓ SQLAlchemy imported")
except Exception as e:
    print("✗ SQLAlchemy error:", e)

try:
    from sentence_transformers import SentenceTransformer
    print("✓ SentenceTransformers imported")
except Exception as e:
    print("✗ SentenceTransformers error:", e)

print("\nAll basic imports successful!")