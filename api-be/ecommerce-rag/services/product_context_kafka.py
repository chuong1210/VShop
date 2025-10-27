"""
Kafka Consumer để auto-sync product context khi có thay đổi
"""
import json
from confluent_kafka import Consumer, KafkaException, KafkaError
from config.config import Config
from services.tools import invalidate_product_cache

class ProductContextKafkaConsumer:
    def __init__(self):
        """Khởi tạo Kafka consumer"""
        config = {
            'bootstrap.servers': Config.KAFKA_BROKER,
            'group.id': Config.KAFKA_GROUP_ID,
            'auto.offset.reset': 'earliest',
            'enable.auto.commit': False,
        }
        self.consumer = Consumer(config)
        self.consumer.subscribe([Config.KAFKA_TOPIC])
        print(f"✓ Kafka consumer subscribed to topic: {Config.KAFKA_TOPIC}")

    def execute(self):
        """Main loop để consume messages"""
        try:
            print("🔄 Kafka consumer started, listening for product changes...")
            while True:
                msg = self.consumer.poll(1.0)
                
                if msg is None:
                    continue
                    
                if msg.error():
                    if msg.error().code() == KafkaError._PARTITION_EOF:
                        continue
                    else:
                        raise KafkaException(msg.error())
                else:
                    self.process_message(msg.value().decode('utf-8'))
                    self.consumer.commit()
                    
        except KeyboardInterrupt:
            print("\n⏹ Kafka consumer stopped by user")
        except Exception as e:
            print(f"❌ Kafka consumer error: {e}")
        finally:
            self.consumer.close()
            print("✓ Kafka consumer closed")

    def process_message(self, message_json: str):
        """Xử lý message từ Kafka"""
        try:
            message = json.loads(message_json)
            operation = message.get('Operation', 'UNKNOWN')
            product_data = message.get('Data', {})
            product_id = product_data.get('Id', 'N/A')

            print(f"📦 Product {product_id} {operation} - Invalidating cache...")
            
            # Gọi tool invalidate_product_cache
            result_str = invalidate_product_cache.invoke({})
            result = json.loads(result_str)
            
            if result.get('success'):
                print(f"✓ Cache invalidated: {result.get('message')}")
            else:
                print(f"✗ Error: {result.get('error')}")

        except json.JSONDecodeError as e:
            print(f"❌ Invalid JSON: {e}")
        except Exception as e:
            print(f"❌ Error processing message: {e}")