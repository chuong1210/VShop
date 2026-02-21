"""
Data Generator for Product Recommendation System
Tạo dữ liệu Users, ProductReviews, và ProductReviewMedia hợp lý
"""

import random
import pymongo
from datetime import datetime, timedelta
from bson import ObjectId
import pyodbc

class DataGenerator:
    def __init__(self, mongo_uri, sqlserver_conn_str):
        # MongoDB connection
        self.mongo_client = pymongo.MongoClient(mongo_uri)
        self.db = self.mongo_client['api_be_db']
        self.reviews_collection = self.db['productReviews']
        self.media_collection = self.db['productReviewMedias']
        
        # SQL Server connection
        self.sql_conn = pyodbc.connect(sqlserver_conn_str)
        self.sql_cursor = self.sql_conn.cursor()
        
        # Load existing data from SQL Server
        self.load_sql_data()
    
    def load_sql_data(self):
        """Load Products and Categories from SQL Server"""
        # Load products
        self.sql_cursor.execute("""
            SELECT Id, Name, Price, CategoryId, Status 
            FROM Products 
            WHERE IsDeleted = 0 AND Status = 1
        """)
        self.products = [
            {'Id': row[0], 'Name': row[1], 'Price': row[2], 
             'CategoryId': row[3], 'Status': row[4]}
            for row in self.sql_cursor.fetchall()
        ]
        print(f"Loaded {len(self.products)} products from SQL Server")
        
        # Load categories
        self.sql_cursor.execute("""
            SELECT Id, Name 
            FROM Categories 
            WHERE IsDeleted = 0
        """)
        self.categories = [
            {'Id': row[0], 'Name': row[1]}
            for row in self.sql_cursor.fetchall()
        ]
        print(f"Loaded {len(self.categories)} categories from SQL Server")
    
    def generate_users(self, num_users=500):
        """
        Tạo users trong SQL Server
        Phân bổ:
        - 70% customers (regular users)
        - 20% active reviewers
        - 10% power users (review nhiều)
        """
        print(f"Generating {num_users} users...")
        
        user_types = []
        for i in range(num_users):
            if i < int(num_users * 0.1):
                user_types.append('power')  # Power users
            elif i < int(num_users * 0.3):
                user_types.append('active')  # Active reviewers
            else:
                user_types.append('regular')  # Regular customers
        
        random.shuffle(user_types)
        
        user_ids = []
        for idx, user_type in enumerate(user_types, start=1):
            # Insert Customer
            self.sql_cursor.execute("""
                INSERT INTO Customers (Name, Phone, Email, Address, Gender, CreatedAt)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                f'Customer {idx}',
                f'090{random.randint(1000000, 9999999)}',
                f'customer{idx}@example.com',
                f'Address {idx}, Ho Chi Minh City',
                random.choice(['Male', 'Female']),
                datetime.now()
            ))
            customer_id = self.sql_cursor.execute("SELECT @@IDENTITY").fetchone()[0]
            
            # Insert User
            self.sql_cursor.execute("""
                INSERT INTO Users (UserName, Password, Email, PhoneNumber, Type, 
                                  CustomerId, IsEmailVerified, CreatedAt)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                f'user{idx}',
                'hashed_password',  # In production, hash properly
                f'customer{idx}@example.com',
                f'090{random.randint(1000000, 9999999)}',
                2,  # UserType.User
                customer_id,
                1,
                datetime.now()
            ))
            user_id = int(self.sql_cursor.execute("SELECT @@IDENTITY").fetchone()[0])

            user_ids.append((user_id, user_type))
        
        self.sql_conn.commit()
        print(f"Created {len(user_ids)} users in SQL Server")
        return user_ids
    
    def generate_realistic_reviews(self, user_ids, num_reviews=5000):
        """
        Tạo reviews hợp lý với pattern:
        - Power users: 20-50 reviews
        - Active users: 5-15 reviews
        - Regular users: 1-5 reviews
        - Rating distribution: Normal distribution centered at 4
        """
        print(f"Generating {num_reviews} reviews...")
        
        reviews = []
        review_texts = {
            5: [
                "Sản phẩm tuyệt vời! Rất đáng mua.",
                "Chất lượng xuất sắc, sẽ mua lại!",
                "Hoàn hảo! Vượt ngoài mong đợi.",
                "Rất thích! Mua hàng tuyệt nhất từ trước đến giờ.",
                "Sản phẩm xuất sắc, giá trị vượt trội."
            ],
            4: [
                "Sản phẩm rất tốt, hài lòng với lần mua này.",
                "Chất lượng tốt, đúng như mong đợi.",
                "Hài lòng với sản phẩm.",
                "Sản phẩm ổn, đáng để giới thiệu.",
                "Khá tốt, chỉ cần cải thiện chút thôi."
            ],
            3: [
                "Ổn, sản phẩm ở mức trung bình.",
                "Tạm được nhưng không có gì nổi bật.",
                "Đáp ứng nhu cầu cơ bản.",
                "Chất lượng trung bình so với giá.",
                "Chấp nhận được nhưng có thể tốt hơn."
            ],
            2: [
                "Thất vọng về chất lượng.",
                "Không như mô tả, mong đợi nhiều hơn.",
                "Dưới mức trung bình, không hài lòng.",
                "Chất lượng kém so với giá tiền.",
                "Không khuyên dùng."
            ],
            1: [
                "Sản phẩm tệ, phí tiền.",
                "Chất lượng rất kém, đừng mua.",
                "Hoàn toàn không hài lòng.",
                "Mua hàng tệ nhất từ trước đến giờ.",
                "Trải nghiệm tồi tệ, yêu cầu hoàn tiền."
            ]
        }

        # Phân bổ reviews cho từng user
        for user_id, user_type in user_ids:
            if user_type == 'power':
                num_user_reviews = random.randint(20, 50)
            elif user_type == 'active':
                num_user_reviews = random.randint(5, 15)
            else:
                num_user_reviews = random.randint(1, 5)
            
            # Random products cho user này
            user_products = random.sample(self.products, 
                                         min(num_user_reviews, len(self.products)))
            
            for product in user_products:
                # Rating distribution: 60% rate 4-5, 30% rate 3, 10% rate 1-2
                rand = random.random()
                if rand < 0.60:
                    rating = random.choice([4, 5])
                elif rand < 0.90:
                    rating = 3
                else:
                    rating = random.choice([1, 2])
                
                review_text = random.choice(review_texts[rating])
                
                # Random date trong 6 tháng qua
                days_ago = random.randint(0, 180)
                review_date = datetime.now() - timedelta(days=days_ago)
                
                review = {
                    'productId': product['Id'],
                    'userId': user_id,
                    'rating': rating,
                    'reviewText': review_text,
                    'parentCommentId': None,
                    'isApproved': True,  # 95% approved
                    'createdAt': review_date,
                    'createdBy': f'user{user_id}',
                    'updatedAt': None,
                    'updatedBy': None,
                    'isDeleted': False
                }
                reviews.append(review)
        
        # Insert vào MongoDB
        if reviews:
            result = self.reviews_collection.insert_many(reviews)
            print(f"Inserted {len(result.inserted_ids)} reviews into MongoDB")
        
        return reviews
    
    def generate_review_media(self, reviews, media_ratio=0.3):
        """
        Tạo media cho reviews
        30% reviews có media (images/videos)
        """
        print(f"Generating review media...")
        
        media_items = []
        sample_images = [
            "https://example.com/review/image1.jpg",
            "https://example.com/review/image2.jpg",
            "https://example.com/review/image3.jpg"
        ]
        sample_videos = [
            "https://example.com/review/video1.mp4",
            "https://example.com/review/video2.mp4"
        ]
        
        # Get all review IDs
        review_ids = [r['_id'] for r in self.reviews_collection.find()]
        
        # Select random reviews to add media
        num_media_reviews = int(len(review_ids) * media_ratio)
        selected_reviews = random.sample(review_ids, num_media_reviews)
        
        comment_id = 1
        for review_id in selected_reviews:
            # 80% images, 20% videos
            if random.random() < 0.8:
                media_url = random.choice(sample_images)
                media_type = 0  # Image
            else:
                media_url = random.choice(sample_videos)
                media_type = 1  # Video
            
            media = {
                'commentId': comment_id,
                'mediaUrl': media_url,
                'type': media_type
            }
            media_items.append(media)
            comment_id += 1
        
        if media_items:
            result = self.media_collection.insert_many(media_items)
            print(f"Inserted {len(result.inserted_ids)} media items into MongoDB")
        
        return media_items
    
    def generate_interactions(self, reviews, interaction_ratio=0.4):
        """
        Tạo comments/replies cho reviews
        40% reviews có interactions
        """
        print(f"Generating review interactions...")
        
        interaction_reviews = []
        review_list = list(self.reviews_collection.find())
        
        # Select reviews để add comments
        num_interactions = int(len(review_list) * interaction_ratio)
        parent_reviews = random.sample(review_list, num_interactions)
        
        # Load user IDs from SQL
        self.sql_cursor.execute("SELECT Id FROM Users WHERE IsDeleted = 0")
        user_ids = [row[0] for row in self.sql_cursor.fetchall()]
        
        for parent_review in parent_reviews:
            # 1-3 comments per review
            num_comments = random.randint(1, 3)
            
            for _ in range(num_comments):
                comment = {
                    'productId': parent_review['productId'],
                    'userId': random.choice(user_ids),
                    'rating': parent_review['rating'],  # Same rating as parent
                    'reviewText': random.choice([
                        "I agree with this review!",
                        "Thanks for sharing!",
                        "Helpful review, thanks!",
                        "Same experience here.",
                        "Good point!"
                    ]),
                    'parentCommentId': int(str(parent_review['_id']), 16) % 1000000,
                    'isApproved': True,
                    'createdAt': parent_review['createdAt'] + timedelta(
                        days=random.randint(1, 30)
                    ),
                    'createdBy': f"user{random.choice(user_ids)}",
                    'updatedAt': None,
                    'updatedBy': None,
                    'isDeleted': False
                }
                interaction_reviews.append(comment)
        
        if interaction_reviews:
            result = self.reviews_collection.insert_many(interaction_reviews)
            print(f"Inserted {len(result.inserted_ids)} interaction reviews")
        
        return interaction_reviews
    
    def print_statistics(self):
        """In thống kê dữ liệu đã tạo"""
        print("\n" + "="*50)
        print("DATA GENERATION STATISTICS")
        print("="*50)
        
        # SQL Server stats
        self.sql_cursor.execute("SELECT COUNT(*) FROM Users WHERE IsDeleted = 0")
        user_count = self.sql_cursor.fetchone()[0]
        print(f"Users (SQL Server): {user_count}")
        
        self.sql_cursor.execute("SELECT COUNT(*) FROM Products WHERE IsDeleted = 0")
        product_count = self.sql_cursor.fetchone()[0]
        print(f"Products (SQL Server): {product_count}")
        
        self.sql_cursor.execute("SELECT COUNT(*) FROM Categories WHERE IsDeleted = 0")
        category_count = self.sql_cursor.fetchone()[0]
        print(f"Categories (SQL Server): {category_count}")
        
        # MongoDB stats
        review_count = self.reviews_collection.count_documents({})
        print(f"Product Reviews (MongoDB): {review_count}")
        
        media_count = self.media_collection.count_documents({})
        print(f"Review Media (MongoDB): {media_count}")
        
        # Rating distribution
        print("\nRating Distribution:")
        for rating in range(1, 6):
            count = self.reviews_collection.count_documents({'rating': rating})
            percentage = (count / review_count * 100) if review_count > 0 else 0
            print(f"  {rating} stars: {count} ({percentage:.1f}%)")
        
        # Average reviews per user
        pipeline = [
            {'$group': {'_id': '$userId', 'count': {'$sum': 1}}},
            {'$group': {'_id': None, 'avg': {'$avg': '$count'}}}
        ]
        result = list(self.reviews_collection.aggregate(pipeline))
        avg_reviews = result[0]['avg'] if result else 0
        print(f"\nAverage reviews per user: {avg_reviews:.2f}")
        
        # Most reviewed products
        pipeline = [
            {'$group': {'_id': '$productId', 'count': {'$sum': 1}}},
            {'$sort': {'count': -1}},
            {'$limit': 5}
        ]
        top_products = list(self.reviews_collection.aggregate(pipeline))
        print("\nTop 5 Most Reviewed Products:")
        for prod in top_products:
            print(f"  Product ID {prod['_id']}: {prod['count']} reviews")
        
        print("="*50 + "\n")
    
    def run(self, num_users=500, num_reviews=5000):
        """Chạy toàn bộ quá trình tạo data"""
        print("Starting data generation...")
        print("="*50)
        
        # Step 1: Generate users
        user_ids = self.generate_users(num_users)
        
        # Step 2: Generate reviews
        reviews = self.generate_realistic_reviews(user_ids, num_reviews)
        
        # Step 3: Generate review media
        self.generate_review_media(reviews, media_ratio=0.3)
        
        # Step 4: Generate interactions
        self.generate_interactions(reviews, interaction_ratio=0.4)
        
        # Step 5: Print statistics
        self.print_statistics()
        
        print("Data generation completed successfully!")
    
    def close(self):
        """Đóng connections"""
        self.mongo_client.close()
        self.sql_conn.close()


if __name__ == "__main__":
    # Configuration
    MONGO_URI = "mongodb://root:admin123@localhost:27017/"
    SQL_SERVER_CONN = (
        "DRIVER={ODBC Driver 17 for SQL Server};"
        "SERVER=USER\\MSSQLSERVER01;"
        "DATABASE=SPMK_VSHOP;"
        "UID=sa;"
        "PWD=101204;"
        "TrustServerCertificate=yes;"
    )

    
    # Run generator
    generator = DataGenerator(MONGO_URI, SQL_SERVER_CONN)
    
    try:
        # Generate data: 500 users, ~5000 reviews
        generator.run(num_users=500, num_reviews=5000)
    finally:
        generator.close()