import os
import hashlib
import time
import json
import threading
from typing import List, Optional
from datetime import datetime
from flask import Flask, request, jsonify, make_response
from flask_cors import CORS
from elasticsearch import Elasticsearch, helpers
from sentence_transformers import SentenceTransformer
from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker
from sqlalchemy.exc import SQLAlchemyError
import redis
from confluent_kafka import Consumer, KafkaException, Producer, KafkaError
from recommendation_service import HybridRecommendationService
from pyspark.sql import SparkSession
from pydantic import BaseModel, Field

app = Flask(__name__)
CORS(app)

# Constants
PRODUCT_INDEX = "products"
DB_CONNECTION_STRING = "mssql+pyodbc://sa:101204@USER\\MSSQLSERVER01/SPMK_VSHOP?driver=ODBC+Driver+17+for+SQL+Server&TrustServerCertificate=yes"
ES_HOST = "http://localhost:9200"  # From Docker
REDIS_HOST = "localhost"
REDIS_PORT = 6379
KAFKA_BROKER = "localhost:9092"
KAFKA_TOPIC = "product-changes"
RECOMMENDATION_TOPIC = "recommendation_events"
KAFKA_GROUP = "product-group"

RATE_LIMIT_WINDOW = 3600  # 1 hour in seconds
RATE_LIMIT_MAX = 100  # Queries per hour per user
CACHE_TTL = 1800  # 30 minutes

# Connections
es_client = Elasticsearch(hosts=[ES_HOST])
engine = create_engine(DB_CONNECTION_STRING)
Session = sessionmaker(bind=engine)
redis_client = redis.StrictRedis(host=REDIS_HOST, port=REDIS_PORT, db=0, decode_responses=True)
kafka_producer = Producer({'bootstrap.servers': KAFKA_BROKER})
# Spark for ALS
spark = SparkSession.builder \
    .appName("RecommendationService") \
    .master("local[*]") \
    .config("spark.driver.memory", "2g") \
    .getOrCreate()
embedding_model = SentenceTransformer('keepitreal/vietnamese-sbert')  # 768 dims, good for VN
recommender = HybridRecommendationService(redis_client, es_client, spark)
# Pydantic Models (equivalent to DTOs)
class BaseDto(BaseModel):
    id: Optional[int] = Field(None, alias="Id")
 

class CategoryDto(BaseDto):
    internal_code: Optional[str] = Field(None, alias="InternalCode")
    name: Optional[str] = Field(None, alias="Name")
    icon: Optional[str] = Field(None, alias="Icon")
    parent_id: Optional[int] = Field(None, alias="ParentId")
    parent: Optional['CategoryDto'] = None

class PromotionForProductDto(BaseModel):
    group_products: Optional[List[Optional[int]]] = Field(None, alias="GroupProducts")
    group: Optional[int] = Field(None, alias="Group")

class PromotionDto(BaseDto):
    internal_code: Optional[str] = Field(None, alias="InternalCode")
    name: Optional[str] = Field(None, alias="Name")
    start: Optional[datetime] = Field(None, alias="Start")
    end: Optional[datetime] = Field(None, alias="End")
    limit: Optional[int] = Field(None, alias="Limit")
    discount: Optional[int] = Field(None, alias="Discount")
    percent_max: Optional[int] = Field(None, alias="PercentMax")
    percent: Optional[int] = Field(None, alias="Percent")
    discount_max: Optional[int] = Field(None, alias="DiscountMax")
    type: Optional[int] = Field(None, alias="Type")  # Enum as int
    status: Optional[int] = Field(None, alias="Status")  # Enum as int
    promotion_for_product: Optional[List[PromotionForProductDto]] = Field(None, alias="PromotionForProduct")

class ProductDto(BaseDto):
    internal_code: Optional[str] = Field(None, alias="InternalCode")
    name: Optional[str] = Field(None, alias="Name")
    images: Optional[List[str]] = Field(None, alias="Images")
    price: Optional[float] = Field(None, alias="Price")
    new_price: Optional[float] = Field(None, alias="NewPrice")
    quantity: Optional[int] = Field(None, alias="Quantity")
    describes: Optional[str] = Field(None, alias="Describes")
    feature: Optional[str] = Field(None, alias="Feature")
    specifications: Optional[str] = Field(None, alias="Specifications")
    status: Optional[int] = Field(None, alias="Status")  # Enum ProductStatus as int
    parent_id: Optional[int] = Field(None, alias="ParentId")
    parent: Optional['ProductDto'] = None
    category_id: Optional[int] = Field(None, alias="CategoryId")
    category: Optional[CategoryDto] = None
    promotion_dto: Optional[PromotionDto] = Field(None, alias="PromotionDto")

class Extra(BaseModel):
    CurrentPage: Optional[int]
    TotalPages: int
    TotalCount: int
    PageSize: Optional[int]

class PaginatedResult(BaseModel):
    data: List[ProductDto]
    succeeded: bool
    code: int
    messages: Optional[List[str]] = []
    extra: Extra
from typing import List, Optional,TypeVar,Generic

# Định nghĩa kiểu tổng quát
T = TypeVar("T")

# === Base Result giống Result<T> bên .NET ===
class Result(BaseModel, Generic[T]):
    succeeded: bool
    code: int
    data: Optional[T] = None
    messages: Optional[List[str]] = []

    @staticmethod
    def success(data: Optional[T] = None, code: int = 200):
        """Tạo result thành công"""
        return Result(succeeded=True, code=code, data=data, messages=[])

    @staticmethod
    def failure(messages: List[str], code: int = 400):
        """Tạo result thất bại"""
        return Result(succeeded=False, code=code, messages=messages)


# === Extra class (thông tin phân trang) ===
class Extra(BaseModel):
    CurrentPage: Optional[int]
    TotalPages: int
    TotalCount: int
    PageSize: Optional[int]

    @staticmethod
    def from_pagination(TotalCount: int, CurrentPage: int, PageSize: int):
        TotalPages = (TotalCount + PageSize - 1) // PageSize
        return Extra(
            CurrentPage=CurrentPage,
            TotalPages=TotalPages,
            TotalCount=TotalCount,
            PageSize=PageSize
        )


# === PaginatedResult kế thừa từ Result[List[T]] ===
class PaginatedResult(Result[List[T]]):
    extra: Extra

    @staticmethod
    def success(data: List[T], total_count: int, page: int, page_size: int, code: int = 200):
        """Tạo kết quả phân trang thành công"""
        extra = Extra.from_pagination(total_count, page, page_size)
        return PaginatedResult(
            succeeded=True,
            code=code,
            data=data,
            messages=[],
            extra=extra
        )

    @staticmethod
    def failure(messages: List[str], code: int = 400):
        """Tạo kết quả phân trang thất bại"""
        return PaginatedResult(
            succeeded=False,
            code=code,
            data=[],
            messages=messages,
            extra=Extra(current_page=1, total_pages=0, total_count=0, page_size=0)
        )

# Custom Exceptions
class NotFoundException(Exception):
    pass

class BadRequestException(Exception):
    pass

# Kafka Consumer Class
class ProductKafkaConsumer:
    def __init__(self):
        config = {
            'bootstrap.servers': KAFKA_BROKER,
            'group.id': KAFKA_GROUP,
            'auto.offset.reset': 'earliest',
            'enable.auto.commit': False,
            'enable.partition.eof': True
        }
        self.consumer = Consumer(config)
        self.consumer.subscribe([KAFKA_TOPIC])
        print("Kafka consumer initialized...")

    def execute(self):
        try:
            while True:
                msg = self.consumer.poll(1.0)
                if msg is None:
                    continue
                if msg.error():
                    if msg.error().code() == KafkaError._PARTITION_EOF:
                        print("Reached end of partition")
                    else:
                        raise KafkaException(msg.error())
                else:
                    self.process_message(msg.value().decode('utf-8'))
                    self.consumer.commit()
        except KeyboardInterrupt:
            pass
        finally:
            self.consumer.close()

    def process_message(self, message_json):
        try:
            message = json.loads(message_json)
            operation = message['Operation']
            product_data = message['Data']
            product_id = product_data['Id']

            if operation.lower() in ['added', 'modified']:
                # Fetch full product from DB
                product = self.fetch_product_from_db(product_id)
                if product:
                    # Index to ES
                    es_client.index(index=PRODUCT_INDEX, id=product_id, document=product)
                    print(f"Indexed product {product_id}")
                else:
                    print(f"Product {product_id} not found in DB")

            elif operation.lower() == 'deleted':
                # Delete from ES
                es_client.delete(index=PRODUCT_INDEX, id=product_id)
                print(f"Deleted product {product_id} from ES")

            else:
                print(f"Unknown operation: {operation}")

            # Invalidate cache
            self.remove_product_cache(product_id)

        except json.JSONDecodeError as e:
            print(f"Invalid JSON: {e}")
        except Exception as e:
            print(f"Error processing message: {e}")

    def fetch_product_from_db(self, product_id):
        with Session() as session:
            query = text("""
                SELECT 
                    p.Id, p.InternalCode, p.Name, p.Images, p.Price, p.Quantity, 
                    p.Describes, p.Feature, p.Specifications, p.Type, p.Status, 
                    p.Selling, p.ParentId, p.CategoryId, 
                    c.Name AS CategoryName,
                    p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy, p.IsDeleted
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.Id = :id AND p.IsDeleted = 0
            """)
            row = session.execute(query, {'id': product_id}).first()
            if row:
                product = dict(row)
                product['images'] = json.loads(row.Images) if row.Images else []
                content = f"{row.Name} {row.CategoryName} {row.Feature} {row.Specifications} {row.Describes}"
                product['embedding'] = embedding_model.encode(content).tolist()
                product['category'] = {"name": row.CategoryName}
                return product
            return None

    def remove_product_cache(self, product_id):
        detail_cache_key = f"product:detail:{product_id}"
        redis_client.delete(detail_cache_key)
        print(f"Removed cache for product {product_id}")

# Helper Functions
def get_current_user_type():  # Mock: Replace with actual auth logic
    return request.headers.get('X-User-Type', 'User')

def is_admin_or_super():  # Check if admin
    user_type = get_current_user_type()
    return user_type in ['Admin', 'SuperAdmin']

def get_user_id():  # Mock: Get from header (e.g., auth)
    return request.headers.get('X-User-ID', 'anonymous')

def rate_limit_check(user_id):
    now = time.time()
    key = f"rate_limit:{user_id}"
    redis_client.zremrangebyscore(key, '-inf', now - RATE_LIMIT_WINDOW)
    count = redis_client.zcard(key)
    if count >= RATE_LIMIT_MAX:
        raise BadRequestException("Rate limit exceeded: 100 queries per hour")
    redis_client.zadd(key, {now: now})
    redis_client.expire(key, RATE_LIMIT_WINDOW)

def log_to_kafka(event):
    kafka_producer.produce(RECOMMENDATION_TOPIC, value=json.dumps(event))
    kafka_producer.flush()

def  create_index_if_not_exists() :
    print("Checking if ES index exists...")
    if not es_client.indices.exists(index=PRODUCT_INDEX):
        mapping = {
            "mappings": {
                "properties": {
                    "id": {"type": "integer"},
                    "internalCode": {"type": "keyword"},
                    "name": {"type": "text", "analyzer": "standard"},
                    "images": {"type": "keyword"},
                    "price": {"type": "float"},
                    "quantity": {"type": "integer"},
                    "describes": {"type": "text"},
                    "feature": {"type": "text"},
                    "specifications": {"type": "text"},
                    "type": {"type": "integer"},
                    "status": {"type": "integer"},
                    "selling": {"type": "integer"},
                    "parentId": {"type": "integer"},
                    "categoryId": {"type": "integer"},
                    "category": {
                        "properties": {
                            "name": {"type": "text"}
                        }
                    },
                    "embedding": {
                        "type": "dense_vector",
                        "dims": 768,
                        "index": True,
                        "similarity": "cosine"
                    }
                }
            }
        }
        es_client.indices.create(index=PRODUCT_INDEX, body=mapping)
        print("Created ES index:", PRODUCT_INDEX)
    else:
        print("ES index already exists:", PRODUCT_INDEX)



# =======================================
# ✅ Hàm lấy dữ liệu từ DB để đưa vào ES
# =======================================
def get_products_from_db():
    print("Fetching products from DB for indexing...")
    with Session() as session:
        query = text("""
            SELECT 
                p.Id, p.InternalCode, p.Name, p.Images, p.Price, p.Quantity, 
                p.Describes, p.Feature, p.Specifications, p.Type, p.Status, 
                p.Selling, p.ParentId, p.CategoryId, 
                c.Name AS CategoryName,
                p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy, p.IsDeleted
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.IsDeleted = 0
        """)

        # Chỉ lấy sản phẩm active và có type = 1
        if not is_admin_or_super():
            query = text(query.text + " AND p.Status = 1")
        query = text(query.text + " AND p.Type = 1")

        result = session.execute(query)
        products = []

        for row in result.mappings():
            # ✅ Chuẩn hóa data
            product = {
                "id": row["Id"],
                "internalCode": row["InternalCode"],
                "name": row["Name"],
                "price": float(row["Price"]) if row["Price"] is not None else 0.0,
                "quantity": row["Quantity"],
                "describes": row["Describes"],
                "feature": row["Feature"],
                "specifications": row["Specifications"],
                "status": row["Status"],
                "parentId": row["ParentId"],
                "categoryId": row["CategoryId"],
                "category": {"name": row["CategoryName"]},
                "createdAt": row["CreatedAt"].isoformat() if row["CreatedAt"] else None,
                "updatedAt": row["UpdatedAt"].isoformat() if row["UpdatedAt"] else None
            }

            # ✅ Xử lý images
            if row["Images"]:
                if row["Images"].strip().startswith('['):
                    try:
                        product["images"] = json.loads(row["Images"])
                    except json.JSONDecodeError:
                        product["images"] = [img.strip() for img in row["Images"].split(",") if img.strip()]
                else:
                    product["images"] = [img.strip() for img in row["Images"].split(",") if img.strip()]
            else:
                product["images"] = []

            # ✅ Tạo embedding để phục vụ search semantic
            content = f"{row['Name']} {row['CategoryName']} {row['Feature']} {row['Specifications']} {row['Describes']}"
            product["embedding"] = embedding_model.encode(content).tolist()

            products.append(product)
        print(f"Fetched {len(products)} products from DB",products)
        return products

def apply_promotion_for_product(product_id, price):
    with Session() as session:
        # 1️⃣ Tìm các group chỉ có 1 sản phẩm (uniqueGroups)
        unique_groups_query = text("""
        	    SELECT [Group]
            FROM PromotionProductRequirements
            GROUP BY [Group]
            HAVING COUNT(*) = 1
        """)
        unique_groups = [row["Group"] for row in session.execute(unique_groups_query).mappings().all()]
        if not unique_groups:
            return None, price, None

        # 2️⃣ Lấy danh sách promotion hợp lệ
        promotions_query = text("""
            SELECT 
                pfp.[Group],
                pr.Id, pr.Name, pr.Type, pr.Percent, pr.PercentMax,
                pr.Discount, pr.DiscountMax, pr.Limit, pr.Status,
                pr.Start, pr.[End]
            FROM PromotionProductRequirements pfp
            JOIN Promotions pr ON pr.Id = pfp.PromotionId
            WHERE 
                pfp.ProductId = :pid
                AND pr.Start <= GETDATE()
                AND GETDATE() <= pr.[End]
                AND pr.Limit >= 1
                AND pr.Status = 1 -- PromotionStatus.Approve
                AND pfp.[Group] IN :groups
        """)

        promo_rows = session.execute(promotions_query, {"pid": product_id, "groups": tuple(unique_groups)}).mappings().all()
        if not promo_rows:
            return None, price, None

        # 3️⃣ Duyệt và chọn khuyến mãi có mức giảm cao nhất
        max_discount = 0
        best_promo = None
        best_group = None

        for row in promo_rows:
            promo_type = row["Type"]
            discount = 0

            if promo_type == 1:  # Type = Percent
                discount_calc = price * (row["Percent"] or 0) * 0.01
                if row["DiscountMax"] and discount_calc > row["DiscountMax"]:
                    discount_calc = row["DiscountMax"]
                discount = discount_calc

            elif promo_type == 2:  # Type = Discount
                discount_calc = row["Discount"] or 0
                if row["PercentMax"]:
                    limit = price * (row["PercentMax"] * 0.01)
                    if discount_calc > limit:
                        discount_calc = limit
                discount = discount_calc

            if discount > max_discount:
                max_discount = discount
                best_promo = dict(row)
                best_group = row["Group"]

        # 4️⃣ Tính giá mới
        new_price = max(price - max_discount, 0)
        return best_promo, new_price, best_group


# Routes
# =========================
# ✅ API: Re-index products
# =========================
@app.route('/smw-api/product/index', methods=['POST'])
def index_products():
    try:
        print("==> START INDEXING PRODUCTS")

        # Tạo index nếu chưa có
        create_index_if_not_exists()

        # Lấy dữ liệu từ DB
        products = get_products_from_db()
        print(f"Fetched {len(products)} products from DB")

        # Xóa dữ liệu cũ để tránh trùng
        es_client.delete_by_query(index=PRODUCT_INDEX, body={"query": {"match_all": {}}})
        print("Old index cleared ✅")

        # Bulk index mới
        actions = [{
            "_index": PRODUCT_INDEX,
            "_id": p["id"],
            "_source": p
        } for p in products]

        helpers.bulk(es_client, actions)
        print("✅ Indexed successfully")

        return jsonify({
            "message": "Products indexed successfully",
            "count": len(products)
        }), 200

    except Exception as e:
        print("ERROR during indexing:", e)
        return jsonify({"error": str(e)}), 500


@app.route('/smw-api/product/search', methods=['GET'])
def search_products():
    user_id = get_user_id()
    try:
        rate_limit_check(user_id)
    except BadRequestException as e:
        return jsonify({"error": str(e)}), 429

    search_term = request.args.get('searchKeyword', '').strip()
    page = int(request.args.get('page', 1))
    page_size = int(request.args.get('pageSize', 10))
    from_ = (page - 1) * page_size

    # Log event to Kafka
    event = {
        "event_type": "search_query",
        "user_id": user_id,
        "query": search_term,
        "timestamp": datetime.utcnow().isoformat()
    }
    log_to_kafka(event)

    if not search_term:
        return jsonify({"error": "Search keyword required"}), 400

    # Cache key
    query_hash = hashlib.sha256(search_term.encode()).hexdigest()
    cache_key = f"search:{query_hash}:{page}:{page_size}"
    cached = redis_client.get(cache_key)
    if cached:
        return make_response(cached), 200

    # Query embedding
    query_embedding = embedding_model.encode(search_term).tolist()

    # ES Query
    search_body = {
        "query": {
            "bool": {
                "should": [
                    {
                        "multi_match": {
                            "query": search_term,
                            "fields": ["name^3", "category.name^2", "feature^2", "specifications^2", "describes^1"],
                            "fuzziness": "AUTO",
                            "type": "best_fields"
                        }
                    }
                ]
            }
        },
        "knn": {
            "field": "embedding",
            "query_vector": query_embedding,
            "k": 20,
            "num_candidates": 100
        },
        "_source": ["id", "internalCode", "name", "images", "price", "quantity", "describes", "feature", "specifications", "status", "parentId", "categoryId", "category.name"],
        "from": from_,
        "size": page_size,
        "min_score": 0.1
    }

    response = es_client.search(index=PRODUCT_INDEX, body=search_body)

    if response['hits']['total']['value'] == 0:
        try:
            with Session() as session:
                keyword_pattern = f"%{search_term}%"
                query = text("""
                    SELECT p.*, c.Name AS CategoryName
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryId = c.Id
                    WHERE p.IsDeleted = 0
                    AND (p.Name LIKE :pattern OR c.Name LIKE :pattern OR p.Feature LIKE :pattern OR p.Specifications LIKE :pattern)
                """)
                if not is_admin_or_super():
                    query = text(query.text + " AND p.Status = 1")
                query = text(query.text + " AND p.Type = 1")
                result = session.execute(query, {"pattern": keyword_pattern})
                products = [dict(row) for row in result.mappings()]
                if not products:
                    raise NotFoundException("No matching product found.")

                # Index fallback
                actions = []
                for p in products:
                    content = f"{p['Name']} {p['CategoryName']} {p['Feature']} {p['Specifications']} {p['Describes']}"
                    p['embedding'] = embedding_model.encode(content).tolist()
                    p['category'] = {"name": p.pop('CategoryName')}
                    p['images'] = json.loads(p['Images']) if p['Images'] else []
                    actions.append({
                        '_index': PRODUCT_INDEX,
                        '_id': p['Id'],
                        '_source': p
                    })
                helpers.bulk(es_client, actions)

                hits = {"hits": [{"_source": p} for p in products[from_:from_ + page_size]]}
                total = len(products)
        except NotFoundException as e:
            return jsonify({"error": str(e)}), 404
        except SQLAlchemyError as e:
            return jsonify({"error": "Database error: " + str(e)}), 500
    else:
        hits = response['hits']['hits']
        total = response['hits']['total']['value']

    # Process: Add promo
    product_dtos = []
    for hit in hits:
        print("Processing hit:", hit)
        product = hit['_source']
        # promo, new_price = apply_promotion_for_product(product['id'], product['price'])

        print("product",product)
        promo, new_price, group = apply_promotion_for_product(product['id'], product['price'])

        dto = ProductDto.parse_obj({
    "Id": int(product['id']),
    "InternalCode": product.get('internalCode'),
    "Name": product.get('name'),
    "Images": product.get('images', []),
    "Price": product.get('price'),
    "NewPrice": new_price,
    "Quantity": product.get('quantity'),
    "Describes": product.get('describes'),
    "Feature": product.get('feature'),
    "Specifications": product.get('specifications'),
    "Status": product.get('status'),
    "ParentId": product.get('parentId'),
    "CategoryId": product.get('categoryId'),
    "Category": {"Name": product['category']['name']} if 'category' in product else None,
    "PromotionDto": promo if promo else None
})

        product_dtos.append(dto.dict(by_alias=True))

    paginated = PaginatedResult.success(
        data=product_dtos,
        total_count=total,
        page=page,
        page_size=page_size
    ).dict(by_alias=True)


    # Cache
    redis_client.set(cache_key, json.dumps(paginated), ex=CACHE_TTL)
    print(json.dumps(paginated, indent=2, ensure_ascii=False))

    return jsonify(paginated), 200

@app.route('/smw-api/recommendations/collaborative', methods=['GET'])
def collaborative_recommendations():
    user_id_str = get_user_id()
    if user_id_str == 'anonymous':
        return jsonify({"error": "User ID required"}), 400
    
    try:
        user_id = int(user_id_str)
        if user_id <= 0:
            return jsonify({"error": "Invalid User ID"}), 400
    except ValueError:
        return jsonify({"error": "Invalid User ID format"}), 400
    
    try:
        rate_limit_check(user_id_str)  # Keep str for cache key if needed
    except BadRequestException as e:
        return jsonify({"error": str(e)}), 429
    
    num_recs = int(request.args.get('numRecs', 10))
    page = int(request.args.get('page', 1))
    page_size = int(request.args.get('pageSize', 10))
    
    # Get recs
    rec_ids = recommender.get_collaborative_recommendations(user_id, num_recs * page)
    total = len(rec_ids)
    
    # Fetch details from ES (paginated)
    from_ = (page - 1) * page_size
    product_ids = rec_ids[from_:from_ + page_size]
    if not product_ids:
        return PaginatedResult.failure(["No recommendations available"]).dict(by_alias=True), 200
    
    search_body = {"query": {"terms": {"id": product_ids}}, "_source": ["id", "name", "price", "images", "category.name"]}
    response = es_client.search(index=PRODUCT_INDEX, body=search_body)
    products = response['hits']['hits']
    
    product_dtos = []
    for hit in products:
        product = hit['_source']
        promo, new_price, _ = apply_promotion_for_product(product['id'], product['price'])
        dto = ProductDto.parse_obj({
            "Id": int(product['id']),
            "Name": product.get('name'),
            "Images": product.get('images', []),
            "Price": product.get('price'),
            "NewPrice": new_price,
            "Quantity": 0,  # Fetch if needed
            "Category": {"Name": product['category']['name']} if 'category' in product else None,
            "PromotionDto": promo if promo else None
        })
        product_dtos.append(dto.dict(by_alias=True))
    
    paginated = PaginatedResult.success(
        data=product_dtos,
        total_count=total,
        page=page,
        page_size=page_size
    ).dict(by_alias=True)
    
    return jsonify(paginated), 200

@app.route('/smw-api/recommendations/similar/<int:product_id>', methods=['GET'])
def similar_products(product_id):
    try:
        rate_limit_check(get_user_id())
    except BadRequestException as e:
        return jsonify({"error": str(e)}), 429
    
    num_recs = int(request.args.get('numRecs', 10))
    page = int(request.args.get('page', 1))
    page_size = int(request.args.get('pageSize', 10))
    
    recs = recommender.get_similar_products(product_id, num_recs * page)
    total = len(recs)
    
    from_ = (page - 1) * page_size
    recs_paginated = recs[from_:from_ + page_size]
    
    product_dtos = []
    for product in recs_paginated:
        promo, new_price, _ = apply_promotion_for_product(product['id'], product['price'])
        dto = ProductDto.parse_obj({
            "Id": int(product['id']),
            "Name": product.get('name'),
            "Images": product.get('images', []),
            "Price": product.get('price'),
            "NewPrice": new_price,
            "Quantity": 0,
            "PromotionDto": promo if promo else None
        })
        product_dtos.append(dto.dict(by_alias=True))
    
    paginated = PaginatedResult.success(
        data=product_dtos,
        total_count=total,
        page=page,
        page_size=page_size
    ).dict(by_alias=True)
    
    return jsonify(paginated), 200

@app.route('/smw-api/recommendations/hybrid', methods=['GET'])
def hybrid_recommendations():
    user_id_str = get_user_id()
    if user_id_str == 'anonymous':
        return jsonify({"error": "User ID required"}), 400
    
    try:
        user_id = int(user_id_str)
        if user_id <= 0:
            return jsonify({"error": "Invalid User ID"}), 400
    except ValueError:
        return jsonify({"error": "Invalid User ID format"}), 400
    
    try:
        rate_limit_check(user_id_str)
    except BadRequestException as e:
        return jsonify({"error": str(e)}), 429
    
    num_recs = int(request.args.get('numRecs', 10))
    page = int(request.args.get('page', 1))
    page_size = int(request.args.get('pageSize', 10))
    
    recs = recommender.get_hybrid_recommendations(user_id, num_recs * page)
    total = len(recs)
    
    from_ = (page - 1) * page_size
    recs_paginated = recs[from_:from_ + page_size]
    
    # Fetch full details if needed (simplified; assume recs have basic info)
    product_dtos = []
    for rec in recs_paginated:
        # Fetch from ES if more details needed
        res = es_client.get(index="products", id=rec['id']) if isinstance(rec.get('id'), int) else None
        product = res['_source'] if res and '_source' in res else rec
        promo, new_price, _ = apply_promotion_for_product(product.get('id', 0), product.get('price', 0))
        dto = ProductDto.parse_obj({
            "Id": int(product.get('id', 0)),
            "Name": product.get('name'),
            "Images": product.get('images', []),
            "Price": product.get('price'),
            "NewPrice": new_price,
            "Quantity": 0,
            "PromotionDto": promo if promo else None
        })
        product_dtos.append(dto.dict(by_alias=True))
    
    paginated = PaginatedResult.success(
        data=product_dtos,
        total_count=total,
        page=page,
        page_size=page_size
    ).dict(by_alias=True)
    
    return jsonify(paginated), 200

@app.route('/smw-api/recommendations/track', methods=['POST'])
def track_interaction():
    data = request.json
    user_id_str = data.get('userId', get_user_id())
    product_id_str = data.get('productId')
    interaction_type = data.get('type', 'view')  # view, like, purchase
    
    if not product_id_str or user_id_str == 'anonymous':
        return jsonify({"error": "User ID and Product ID required"}), 400
    
    try:
        user_id = int(user_id_str)
        product_id = int(product_id_str)
        if user_id <= 0 or product_id <= 0:
            return jsonify({"error": "Invalid IDs"}), 400
    except ValueError:
        return jsonify({"error": "Invalid ID format"}), 400
    
    success = recommender.track_user_interaction(user_id, product_id, interaction_type)
    
    if success:
        # Log to Kafka
        event = {
            "event_type": "user_interaction",
            "user_id": user_id_str,
            "product_id": product_id_str,
            "type": interaction_type,
            "timestamp": datetime.utcnow().isoformat()
        }
        log_to_kafka(event)
        return jsonify({"message": "Interaction tracked successfully"}), 200
    else:
        return jsonify({"error": "Failed to track interaction"}), 500

@app.route('/smw-api/recommendations/trending', methods=['GET'])
def trending_products():
    try:
        rate_limit_check(get_user_id())
    except BadRequestException as e:
        return jsonify({"error": str(e)}), 429
    
    num_recs = int(request.args.get('numRecs', 10))
    page = int(request.args.get('page', 1))
    page_size = int(request.args.get('pageSize', 10))
    
    recs = recommender.get_trending_products(num_recs * page)
    total = len(recs)
    
    from_ = (page - 1) * page_size
    recs_paginated = recs[from_:from_ + page_size]
    
    product_dtos = []
    for product in recs_paginated:
        promo, new_price, _ = apply_promotion_for_product(product.get('id', 0), product.get('price', 0))
        dto = ProductDto.parse_obj({
            "Id": int(product.get('id', 0)),
            "Name": product.get('name'),
            "Images": product.get('images', []),
            "Price": product.get('price'),
            "NewPrice": new_price,
            "Quantity": 0,
            "PromotionDto": promo if promo else None
        })
        product_dtos.append(dto.dict(by_alias=True))
    
    paginated = PaginatedResult.success(
        data=product_dtos,
        total_count=total,
        page=page,
        page_size=page_size
    ).dict(by_alias=True)
    
    return jsonify(paginated), 200
@app.route('/smw-api/product/start', methods=['POST'])
def start_consumer():
    return jsonify({"message": "Consumer is running"}), 200

if __name__ == '__main__':
    create_index_if_not_exists()
    consumer = ProductKafkaConsumer()
    consumer_thread = threading.Thread(target=consumer.execute)
    consumer_thread.daemon = True
    consumer_thread.start()
    app.run(debug=True)