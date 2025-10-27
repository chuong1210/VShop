import requests
from typing import List, Dict
from config.config import Config

class RecommendationService:
    def __init__(self):
        self.api_url = Config.RECOMMENDATION_API_URL
    
    def get_recommendations(self, context: str, user_id: str = None, limit: int = 5) -> List[Dict]:
        """Gọi API gợi ý sản phẩm từ Flask+Spark"""
        try:
            payload = {
                "context": context,
                "user_id": user_id,
                "limit": limit
            }
            
            response = requests.post(
                self.api_url,
                json=payload,
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                return data.get("recommendations", [])
            else:
                print(f"Recommendation API error: {response.status_code}")
                return []
                
        except requests.exceptions.Timeout:
            print("Recommendation API timeout")
            return []
        except requests.exceptions.RequestException as e:
            print(f"Error calling recommendation API: {e}")
            return []
        except Exception as e:
            print(f"Unexpected error in recommendation service: {e}")
            return []