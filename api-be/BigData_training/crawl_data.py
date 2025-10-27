import requests
import json
import os
import time
from pathlib import Path
import re

class GearVNToSQLCrawler:
    def __init__(self, 
                 base_path="C:\\Users\\chuon\\Desktop\\KhoaLuan\\Images",
                 sql_output_path="C:\\Users\\chuon\\Desktop\\VShop\\api-be"):
        self.base_path = Path(base_path)
        self.sql_output_path = Path(sql_output_path)
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
        })
        
        # Tạo thư mục nếu chưa tồn tại
        self.base_path.mkdir(parents=True, exist_ok=True)
        self.sql_output_path.mkdir(parents=True, exist_ok=True)
        
        # Tracking
        self.crawled_product_ids = set()
        self.category_mapping = {}  # Mapping từ category key sang SQL Id
        self.category_counter = 1
        self.product_counter = 1
        
        # SQL data storage
        self.sql_categories = []
        self.sql_products = []
        
        # Categories structure
        self.categories = {
            "LAPTOP": {
                "name": "Laptop",
                "order": 1,
                "icon": "laptop-icon.png",
                "sub_categories": [
                    {
                        "name": "Laptop Văn Phòng Bán Chạy",
                        "url": "https://gearvn.com/collections/laptop-van-phong-ban-chay/products.json",
                        "code": "LAPTOP_VP"
                    },
                    {
                        "name": "Laptop Dưới 15 Triệu",
                        "url": "https://gearvn.com/collections/laptop-hoc-tap-va-lam-viec-duoi-15tr/products.json",
                        "code": "LAPTOP_U15"
                    },
                    {
                        "name": "Laptop 15-20 Triệu",
                        "url": "https://gearvn.com/collections/laptop-hoc-tap-va-lam-viec-tu-15tr-den-20tr/products.json",
                        "code": "LAPTOP_15_20"
                    },
                    {
                        "name": "Laptop Trên 20 Triệu",
                        "url": "https://gearvn.com/collections/laptop-hoc-tap-va-lam-viec-tren-20-trieu/products.json",
                        "code": "LAPTOP_O20"
                    }
                ]
            },
            "LAPTOP_GAMING": {
                "name": "Laptop Gaming",
                "order": 2,
                "icon": "laptop-gaming-icon.png",
                "sub_categories": [
                    {
                        "name": "Gaming Bán Chạy",
                        "url": "https://gearvn.com/collections/laptop-gaming-ban-chay/products.json",
                        "code": "GAMING_BC"
                    },
                    {
                        "name": "Gaming 20-25 Triệu",
                        "url": "https://gearvn.com/collections/laptop-gaming-gia-tu-20-den-25-trieu/products.json",
                        "code": "GAMING_20_25"
                    },
                    {
                        "name": "Gaming 25-35 Triệu",
                        "url": "https://gearvn.com/collections/laptop-gaming-gia-tu-25-den-35-trieu/products.json",
                        "code": "GAMING_25_35"
                    },
                    {
                        "name": "Gaming Trên 35 Triệu",
                        "url": "https://gearvn.com/collections/laptop-gaming-tren-35-trieu/products.json",
                        "code": "GAMING_O35"
                    }
                ]
            },
            "PC_GVN": {
                "name": "PC GVN",
                "order": 3,
                "icon": "pc-icon.png",
                "sub_categories": [
                    {
                        "name": "PC GVN i3",
                        "url": "https://gearvn.com/collections/pc-gvn-i3/products.json",
                        "code": "PC_I3"
                    },
                    {
                        "name": "PC GVN i5",
                        "url": "https://gearvn.com/collections/pc-gvn-i5/products.json",
                        "code": "PC_I5"
                    },
                    {
                        "name": "PC GVN i7",
                        "url": "https://gearvn.com/collections/pc-gvn-i7/products.json",
                        "code": "PC_I7"
                    },
                    {
                        "name": "PC GVN i9",
                        "url": "https://gearvn.com/collections/pc-gvn-i9/products.json",
                        "code": "PC_I9"
                    }
                ]
            },
            "LINH_KIEN": {
                "name": "Linh Kiện",
                "order": 4,
                "icon": "component-icon.png",
                "sub_categories": [
                    {
                        "name": "VGA Card",
                        "url": "https://gearvn.com/collections/vga-rtx-50-series/products.json",
                        "code": "VGA"
                    },
                    {
                        "name": "Mainboard",
                        "url": "https://gearvn.com/collections/mainboard-bo-mach-chu/products.json",
                        "code": "MAIN"
                    },
                    {
                        "name": "CPU",
                        "url": "https://gearvn.com/collections/cpu-amd-ryzen/products.json",
                        "code": "CPU"
                    },
                    {
                        "name": "Case",
                        "url": "https://gearvn.com/collections/case-thung-may-tinh/products.json",
                        "code": "CASE"
                    },
                    {
                        "name": "Nguồn",
                        "url": "https://gearvn.com/collections/psu-nguon-may-tinh/products.json",
                        "code": "PSU"
                    },
                    {
                        "name": "Tản Nhiệt",
                        "url": "https://gearvn.com/collections/tan-nhiet-may-tinh/products.json",
                        "code": "COOLING"
                    }
                ]
            },
            "STORAGE": {
                "name": "Ổ Cứng & RAM",
                "order": 5,
                "icon": "storage-icon.png",
                "sub_categories": [
                    {
                        "name": "RAM",
                        "url": "https://gearvn.com/collections/ram-pc/products.json",
                        "code": "RAM"
                    },
                    {
                        "name": "HDD",
                        "url": "https://gearvn.com/collections/hdd-o-cung-pc/products.json",
                        "code": "HDD"
                    },
                    {
                        "name": "SSD",
                        "url": "https://gearvn.com/collections/ssd-o-cung-the-ran/products.json",
                        "code": "SSD"
                    }
                ]
            },
            "NGOAI_VI": {
                "name": "Thiết Bị Ngoại Vi",
                "order": 6,
                "icon": "peripheral-icon.png",
                "sub_categories": [
                    {
                        "name": "Màn Hình",
                        "url": "https://gearvn.com/collections/man-hinh-oled/products.json",
                        "code": "MONITOR"
                    },
                    {
                        "name": "Bàn Phím",
                        "url": "https://gearvn.com/collections/ban-phim-may-tinh/products.json",
                        "code": "KEYBOARD"
                    },
                    {
                        "name": "Chuột",
                        "url": "https://gearvn.com/collections/chuot-may-tinh/products.json",
                        "code": "MOUSE"
                    },
                    {
                        "name": "Tai Nghe",
                        "url": "https://gearvn.com/collections/tai-nghe-may-tinh/products.json",
                        "code": "HEADSET"
                    }
                ]
            }
        }

    def clean_sql_string(self, text):
        """Làm sạch chuỗi cho SQL"""
        if text is None:
            return ''
        
        # Chuyển sang string nếu không phải
        text = str(text)
        
        # Thay thế dấu nháy đơn
        text = text.replace("'", "''")
        
        # Loại bỏ ký tự đặc biệt
        text = text.replace('\r\n', ' ').replace('\n', ' ').replace('\r', ' ')
        
        # Giới hạn độ dài
        return text[:500] if len(text) > 500 else text

    def download_image(self, image_url, folder_path, product_handle, image_index):
        """Tải ảnh"""
        try:
            file_extension = os.path.splitext(image_url)[1]
            if not file_extension:
                file_extension = '.jpg'
            
            filename = f"{product_handle}_{image_index}{file_extension}"
            filename = re.sub(r'[<>:"/\\|?*]', '_', filename)[:200]
            file_path = folder_path / filename
            
            if file_path.exists():
                return str(file_path)
            
            response = self.session.get(image_url, timeout=30)
            response.raise_for_status()
            
            with open(file_path, 'wb') as f:
                f.write(response.content)
            
            print(f"      Downloaded: {filename}")
            return str(file_path)
            
        except Exception as e:
            print(f"      Error downloading {image_url}: {str(e)}")
            return None

    def get_product_detail(self, product_handle):
        """Lấy chi tiết sản phẩm"""
        try:
            detail_url = f"https://gearvn.com/products/{product_handle}.js"
            response = self.session.get(detail_url, timeout=15)
            response.raise_for_status()
            return response.json()
        except Exception as e:
            print(f"      Error getting detail: {str(e)}")
            return None

    def extract_specs(self, tags):
        """Trích xuất thông số từ tags"""
        if not tags:
            return {}
        
        specs = {}
        for tag in tags:
            if ':' in tag:
                key, value = tag.split(':', 1)
                if key.startswith('hl_') or key.startswith('spec_'):
                    specs[key] = value
        
        return specs

    def crawl_category(self, category_key, category_info, parent_id=None):
        """Crawl một category"""
        print(f"\n{'='*60}")
        print(f"Category: {category_info['name']}")
        print(f"{'='*60}")
        
        # Insert category chính
        cat_id = self.category_counter
        self.category_counter += 1
        
        self.sql_categories.append({
            'Id': cat_id,
            'InternalCode': category_key,
            'Name': category_info['name'],
            'Icon': category_info.get('icon'),
            'ParentId': parent_id
        })
        
        self.category_mapping[category_key] = cat_id
        
        # Crawl sub-categories
        for sub_cat in category_info.get('sub_categories', []):
            self.crawl_subcategory(sub_cat, cat_id, category_key)
            time.sleep(2)

    def crawl_subcategory(self, sub_cat, parent_id, parent_key):
        """Crawl sub-category và sản phẩm"""
        print(f"\nSub-category: {sub_cat['name']}")
        
        # Insert sub-category
        sub_cat_id = self.category_counter
        self.category_counter += 1
        
        sub_cat_key = f"{parent_key}_{sub_cat['code']}"
        
        self.sql_categories.append({
            'Id': sub_cat_id,
            'InternalCode': sub_cat['code'],
            'Name': sub_cat['name'],
            'Icon': None,
            'ParentId': parent_id
        })
        
        self.category_mapping[sub_cat_key] = sub_cat_id
        
        # Crawl products
        self.crawl_products(sub_cat['url'], sub_cat_id, sub_cat['name'])

    def crawl_products(self, api_url, category_id, category_name, max_pages=5):
        """Crawl sản phẩm từ API"""
        page = 1
        total_products = 0
        
        while page <= max_pages:
            try:
                url = f"{api_url}?include=metafields[product]&page={page}&limit=12"
                print(f"  Page {page}: {url}")
                
                response = self.session.get(url, timeout=30)
                response.raise_for_status()
                
                data = response.json()
                
                # Xử lý response
                products = []
                if isinstance(data, dict) and 'products' in data:
                    products = data['products']
                elif isinstance(data, list):
                    products = data
                
                if not products:
                    print(f"    No products on page {page}")
                    break
                
                print(f"    Found {len(products)} products")
                
                # Xử lý từng sản phẩm
                for product in products:
                    try:
                        # Kiểm tra product hợp lệ
                        if not isinstance(product, dict):
                            continue
                        
                        product_id = product.get('id')
                        if not product_id or product_id in self.crawled_product_ids:
                            continue
                        
                        self.crawled_product_ids.add(product_id)
                        
                        # Lấy thông tin cơ bản
                        name = product.get('title', 'Unknown Product')
                        handle = product.get('handle', '')
                        vendor = product.get('vendor', '')
                        product_type = product.get('product_type', '')
                        
                        # Lấy giá từ variant đầu tiên
                        variants = product.get('variants', [])
                        price = 0
                        quantity = 0
                        
                        if variants and len(variants) > 0:
                            first_variant = variants[0]
                            if isinstance(first_variant, dict):
                                try:
                                    price = float(first_variant.get('price', 0)) / 100  # Convert từ cent sang đồng
                                except:
                                    price = 0
                                
                                quantity = first_variant.get('inventory_quantity', 0) or 0
                        
                        # Lấy ảnh
                        images = []
                        image_list = product.get('images', [])
                        if image_list:
                            for img in image_list[:3]:  # Lấy tối đa 3 ảnh
                                if isinstance(img, dict) and img.get('src'):
                                    images.append(img['src'])
                        
                        images_str = ','.join(images) if images else ''
                        
                        # Lấy thông số kỹ thuật
                        tags = product.get('tags', [])
                        if isinstance(tags, str):
                            tags = tags.split(',')
                        
                        specs = self.extract_specs(tags)
                        specs_str = json.dumps(specs, ensure_ascii=False) if specs else ''
                        
                        # Mô tả (lấy từ body_html nếu có, nhưng rút gọn)
                        description = ''
                        body_html = product.get('body_html', '')
                        if body_html:
                            # Loại bỏ HTML tags
                            description = re.sub(r'<[^>]+>', '', str(body_html))
                            description = description.strip()[:1000]
                        
                        # Feature (từ vendor và type)
                        feature = f"{vendor} - {product_type}" if vendor or product_type else ''
                        
                        # Thêm vào SQL data
                        sql_product_id = self.product_counter
                        self.product_counter += 1
                        
                        self.sql_products.append({
                            'Id': sql_product_id,
                            'InternalCode': handle[:50],
                            'Name': name[:100],
                            'Images': images_str[:500],
                            'Price': price,
                            'Quantity': quantity,
                            'Describes': description[:1000],
                            'Feature': feature[:500],
                            'Specifications': specs_str[:500],
                            'Type': 1,  # Default product type
                            'Status': 1 if product.get('available', False) else 0,
                            'Selling': 0,  # Default
                            'ParentId': None,
                            'CategoryId': category_id
                        })
                        
                        total_products += 1
                        
                        print(f"      ✓ {name[:50]}")
                        
                    except Exception as e:
                        print(f"      Error processing product: {str(e)}")
                        continue
                
                print(f"    Crawled {total_products} products")
                
                page += 1
                time.sleep(1)
                
            except Exception as e:
                print(f"    Error on page {page}: {str(e)}")
                break
        
        return total_products

    def generate_sql_file(self):
        """Tạo file SQL"""
        sql_file = self.sql_output_path / "insert_gearvn_data.sql"
        
        print(f"\n{'='*60}")
        print("Generating SQL file...")
        print(f"{'='*60}")
        
        with open(sql_file, 'w', encoding='utf-8') as f:
            # Header
            f.write("-- GearVN Data Import Script\n")
            f.write(f"-- Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"-- Total Categories: {len(self.sql_categories)}\n")
            f.write(f"-- Total Products: {len(self.sql_products)}\n")
            f.write("\n")
            f.write("-- Disable constraints for faster insertion\n")
            f.write("SET IDENTITY_INSERT [Categories] ON;\n")
            f.write("GO\n\n")
            
            # Insert Categories
            f.write("-- =============================================\n")
            f.write("-- INSERT CATEGORIES\n")
            f.write("-- =============================================\n\n")
            
            for cat in self.sql_categories:
                parent_id = 'NULL' if cat['ParentId'] is None else str(cat['ParentId'])
                icon = f"'{self.clean_sql_string(cat['Icon'])}'" if cat['Icon'] else 'NULL'
                
                sql = f"""INSERT INTO [Categories] ([Id], [InternalCode], [Name], [Icon], [ParentId], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES ({cat['Id']}, '{self.clean_sql_string(cat['InternalCode'])}', N'{self.clean_sql_string(cat['Name'])}', {icon}, {parent_id}, GETDATE(), GETDATE(), 0);
"""
                f.write(sql)
            
            f.write("\nSET IDENTITY_INSERT [Categories] OFF;\n")
            f.write("GO\n\n")
            
            # Insert Products
            f.write("SET IDENTITY_INSERT [Products] ON;\n")
            f.write("GO\n\n")
            
            f.write("-- =============================================\n")
            f.write("-- INSERT PRODUCTS\n")
            f.write("-- =============================================\n\n")
            
            batch_size = 100
            for i, prod in enumerate(self.sql_products):
                parent_id = 'NULL' if prod['ParentId'] is None else str(prod['ParentId'])
                
                sql = f"""INSERT INTO [Products] ([Id], [InternalCode], [Name], [Images], [Price], [Quantity], [Describes], [Feature], [Specifications], [Type], [Status], [Selling], [ParentId], [CategoryId], [CreatedAt], [UpdatedAt], [IsDeleted])
VALUES ({prod['Id']}, '{self.clean_sql_string(prod['InternalCode'])}', N'{self.clean_sql_string(prod['Name'])}', N'{self.clean_sql_string(prod['Images'])}', {prod['Price']}, {prod['Quantity']}, N'{self.clean_sql_string(prod['Describes'])}', N'{self.clean_sql_string(prod['Feature'])}', N'{self.clean_sql_string(prod['Specifications'])}', {prod['Type']}, {prod['Status']}, {prod['Selling']}, {parent_id}, {prod['CategoryId']}, GETDATE(), GETDATE(), 0);
"""
                f.write(sql)
                
                # Batch commit
                if (i + 1) % batch_size == 0:
                    f.write("GO\n\n")
            
            f.write("\nSET IDENTITY_INSERT [Products] OFF;\n")
            f.write("GO\n\n")
            
            # Footer
            f.write("-- =============================================\n")
            f.write("-- SUMMARY\n")
            f.write("-- =============================================\n")
            f.write(f"-- Categories inserted: {len(self.sql_categories)}\n")
            f.write(f"-- Products inserted: {len(self.sql_products)}\n")
            f.write("-- Script completed successfully!\n")
        
        print(f"✓ SQL file created: {sql_file}")
        print(f"  - Categories: {len(self.sql_categories)}")
        print(f"  - Products: {len(self.sql_products)}")

    def run(self):
        """Chạy crawler"""
        print("GEARVN TO SQL SERVER CRAWLER")
        print("="*60)
        
        start_time = time.time()
        
        try:
            # Crawl từng category
            for cat_key, cat_info in self.categories.items():
                self.crawl_category(cat_key, cat_info)
            
            # Generate SQL file
            self.generate_sql_file()
            
            # Summary
            duration = time.time() - start_time
            print(f"\n{'='*60}")
            print("COMPLETED!")
            print(f"{'='*60}")
            print(f"Time: {duration/60:.1f} minutes")
            print(f"Categories: {len(self.sql_categories)}")
            print(f"Products: {len(self.sql_products)}")
            print(f"SQL File: {self.sql_output_path / 'insert_gearvn_data.sql'}")
            
        except Exception as e:
            print(f"\nError: {str(e)}")
            import traceback
            traceback.print_exc()

def main():
    crawler = GearVNToSQLCrawler()
    crawler.run()

if __name__ == "__main__":
    main()