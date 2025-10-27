import pyodbc
import pandas as pd
from typing import List, Dict, Optional
from config.config import Config

class DatabaseService:
    def __init__(self):
        self.conn_string = Config.SQL_SERVER_CONN
        self.conn = "DRIVER={ODBC Driver 17 for SQL Server};SERVER=USER\\MSSQLSERVER01;DATABASE=SPMK_VSHOP;UID=sa;PWD=101204;TrustServerCertificate=yes;"

    
    def get_connection(self):
        """Tạo kết nối đến SQL Server"""
        return pyodbc.connect(self.conn)
    
    def get_categories(self) -> List[Dict]:
        """Lấy danh sách danh mục"""
        query = """
        SELECT Id, Name, Icon, ParentId 
        FROM Categories 
        WHERE IsDeleted = 0
        ORDER BY Name
        """
        with self.get_connection() as conn:
            df = pd.read_sql(query, conn)
            return df.to_dict('records')
    
    def get_products(self, category_id: Optional[int] = None, 
                     search_term: Optional[str] = None,
                     limit: int = 20) -> List[Dict]:
        """Lấy danh sách sản phẩm"""
        query = """
        SELECT 
            p.Id, p.InternalCode, p.Name, p.Images, p.Price, 
            p.Quantity, p.Describes, p.Feature, p.Specifications,
            p.Status, p.Selling, c.Name as CategoryName
        FROM Products p
        LEFT JOIN Categories c ON p.CategoryId = c.Id
        WHERE p.IsDeleted = 0
        """
        
        params = []
        if category_id:
            query += " AND p.CategoryId = ?"
            params.append(category_id)
        
        if search_term:
            query += " AND (p.Name LIKE ? OR p.Describes LIKE ?)"
            search_pattern = f"%{search_term}%"
            params.extend([search_pattern, search_pattern])
        
        query += f" ORDER BY p.Selling DESC, p.CreatedAt DESC OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY"
        
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, params)
            columns = [column[0] for column in cursor.description]
            results = []
            for row in cursor.fetchall():
                results.append(dict(zip(columns, row)))
            return results
    
    def get_product_by_id(self, product_id: int) -> Optional[Dict]:
        """Lấy thông tin chi tiết sản phẩm"""
        query = """
        SELECT 
            p.*, c.Name as CategoryName
        FROM Products p
        LEFT JOIN Categories c ON p.CategoryId = c.Id
        WHERE p.Id = ? AND p.IsDeleted = 0
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [product_id])
            columns = [column[0] for column in cursor.description]
            row = cursor.fetchone()
            if row:
                return dict(zip(columns, row))
            return None
    
    def get_products_for_rag_context(self) -> str:
        """Lấy thông tin sản phẩm để nhúng vào context RAG"""
        query = """
        SELECT 
            p.Name, p.Price, p.Describes, p.Feature, 
            c.Name as CategoryName
        FROM Products p
        LEFT JOIN Categories c ON p.CategoryId = c.Id
        WHERE p.IsDeleted = 0 AND p.Status = 1
        ORDER BY p.Selling DESC
        """
        with self.get_connection() as conn:
            df = pd.read_sql(query, conn)
            
        # Tạo text context từ products
        context_parts = []
        for _, row in df.iterrows():
            product_text = f"""
Sản phẩm: {row['Name']}
Danh mục: {row['CategoryName']}
Giá: {row['Price']:,.0f} VNĐ
Mô tả: {row['Describes']}
Đặc điểm: {row['Feature']}
"""
            context_parts.append(product_text.strip())
        
        return "\n\n".join(context_parts)
    def get_cart_id_for_customer(self, customer_id: int) -> Optional[int]:
        """Lấy ID giỏ hàng (Order với Status=0) cho customer"""
        query = """
        SELECT Id FROM Orders 
        WHERE CustomerId = ? AND Status = 0 AND IsDeleted = 0
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [customer_id])
            row = cursor.fetchone()
            if row:
                return row[0]
        return None

    def create_cart_for_customer(self, customer_id: int) -> int:
        """Tạo giỏ hàng mới nếu chưa có"""
        query = """
        INSERT INTO Orders (CustomerId, TotalAmount, TotalDecrease, Total, Status, Type, IsDeleted, CreatedAt)
        VALUES (?, 0, 0, 0, 0, 1, 0, GETDATE());  -- Status=0 (Cart), Type=1 (Online)
        SELECT SCOPE_IDENTITY();
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [customer_id])
            cart_id = cursor.fetchone()[0]
            conn.commit()
            return cart_id

    def add_to_cart_db(self, cart_id: int, product_id: int, quantity: int):
        """Thêm hoặc cập nhật sản phẩm vào giỏ hàng trong DB"""
        # Kiểm tra tồn kho (tương tự .NET, nhưng đơn giản)
        product = self.get_product_by_id(product_id)
        if not product or quantity > product['Quantity']:
            raise ValueError("Số lượng sản phẩm tồn kho không đủ!")

        query_check = """
        SELECT Id, Quantity FROM DetailOrders 
        WHERE OrderId = ? AND ProductId = ?
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query_check, [cart_id, product_id])
            row = cursor.fetchone()
            if row:
                new_quantity = row[1] + quantity
                query_update = """
                UPDATE DetailOrders SET Quantity = ? WHERE Id = ?
                """
                cursor.execute(query_update, [new_quantity, row[0]])
            else:
                price = product['Price']
                query_insert = """
                INSERT INTO DetailOrders (OrderId, ProductId, Quantity, Cost, Price, IsSelected)
                VALUES (?, ?, ?, ?, ?, 1)  -- Cost=Price ban đầu, IsSelected=1
                """
                cursor.execute(query_insert, [cart_id, product_id, quantity, price, price])
            conn.commit()

        # Cập nhật tổng tiền Order (tương tự HandleAfterUpdateProductToCart ở .NET)
        self.update_cart_totals(cart_id)

    def update_cart_item_quantity(self, cart_id: int, product_id: int, quantity: int):
        """Cập nhật số lượng sản phẩm"""
        if quantity < 1:
            self.remove_from_cart_db(cart_id, product_id)
            return
        query = """
        UPDATE DetailOrders SET Quantity = ? 
        WHERE OrderId = ? AND ProductId = ?
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [quantity, cart_id, product_id])
            conn.commit()
        self.update_cart_totals(cart_id)

    def remove_from_cart_db(self, cart_id: int, product_id: int):
        """Xóa sản phẩm khỏi giỏ"""
        query = """
        DELETE FROM DetailOrders 
        WHERE OrderId = ? AND ProductId = ?
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [cart_id, product_id])
            conn.commit()
        self.update_cart_totals(cart_id)

    def get_cart_items(self, cart_id: int) -> List[Dict]:
        """Lấy danh sách items trong giỏ"""
        query = """
        SELECT d.Id, p.Id as product_id, p.Name as name, d.Quantity as quantity, d.Price as price, 
               p.Images as image
        FROM DetailOrders d 
        JOIN Products p ON d.ProductId = p.Id
        WHERE d.OrderId = ?
        """
        with self.get_connection() as conn:
            df = pd.read_sql(query, conn, params=[cart_id])
            return df.to_dict('records')

    def update_cart_totals(self, cart_id: int):
        """Cập nhật tổng tiền cho Order (tương tự .NET)"""
        query_totals = """
        UPDATE o
        SET o.Total = (SELECT SUM(d.Price * d.Quantity) FROM DetailOrders d WHERE d.OrderId = o.Id),
            o.TotalAmount = (SELECT SUM(d.Price * d.Quantity) FROM DetailOrders d WHERE d.OrderId = o.Id),
            o.TotalDecrease = 0  -- Chưa xử lý discount, có thể thêm sau
        FROM Orders o WHERE o.Id = ?
        """
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query_totals, [cart_id])
            conn.commit()

    def clear_cart(self, cart_id: int):
        """Xóa toàn bộ giỏ"""
        query = "DELETE FROM DetailOrders WHERE OrderId = ?"
        with self.get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, [cart_id])
            conn.commit()
        self.update_cart_totals(cart_id)