USE SPMK_VSHOP;
GO

-------------------------------------------------
-- 1️⃣ CATEGORIES
-------------------------------------------------
INSERT INTO Categories (InternalCode, Name, Icon, ParentId)
VALUES 
    ('CAT001', 'Electronics', 'electronics_icon.png', NULL), -- ID = 1
    ('CAT002', 'Home Appliances', 'home_appliances_icon.png', NULL); -- ID = 2

-- Subcategories (tham chiếu ParentId là ID của 2 root trên)
INSERT INTO Categories (InternalCode, Name, Icon, ParentId)
VALUES 
    ('CAT003', 'Smartphones', 'smartphones_icon.png', 1),
    ('CAT004', 'Televisions', 'televisions_icon.png', 1),
    ('CAT005', 'Washing Machines', 'washing_machines_icon.png', 2),
    ('CAT006', 'Refrigerators', 'refrigerators_icon.png', 2),
    ('CAT007', 'Laptops', 'laptops_icon.png', 1),
    ('CAT008', 'Kitchen Appliances', 'kitchen_appliances_icon.png', 2),
    ('CAT009', 'Gaming Consoles', 'gaming_consoles_icon.png', 1),
    ('CAT010', 'Microwaves', 'microwaves_icon.png', 2);
GO

-------------------------------------------------
-- 2️⃣ PRODUCTS
-------------------------------------------------
INSERT INTO Products 
(InternalCode, Name, Images, Price, Quantity, Describes, Feature, Specifications, Type, Status, Selling, ParentId, CategoryId, CreatedAt, UpdatedAt)
VALUES 
    -- Smartphones (CategoryId = 3)
    ('PROD001', 'iPhone 14 Pro Max', 
     'https://res.cloudinary.com/dqxh4rmi3/image/upload/v1737270570/supermarket/qvhfdr6irqlexgrvpdrg.jpg,https://res.cloudinary.com/dqxh4rmi3/image/upload/v1737624270/iphone-14-pro-max-gold-0-60ea017_vzbwaa.jpg',
     1200.00, 50, 'Latest iPhone with A16 Bionic chip', '5G, 120Hz Display', '256GB, 6GB RAM', 1, 1, 20, NULL, 3, GETDATE(), GETDATE()),

    ('PROD002', 'Samsung Galaxy S23 Ultra', 
     'https://sm.pcmag.com/t/pcmag_me/photo/default/s23-ultra-18_n5vf.1920.jpg',
     1100.00, 40, 'Flagship Samsung smartphone with Snapdragon 8 Gen 2', '200MP Camera, S-Pen', '512GB, 12GB RAM', 1, 1, 15, NULL, 3, GETDATE(), GETDATE()),

    -- Televisions (CategoryId = 4)
    ('PROD003', 'LG OLED C3', 
     'https://m.media-amazon.com/images/I/71O7wgely5L.jpg',
     1500.00, 30, '4K OLED TV with Dolby Vision', '120Hz Refresh Rate', '55 inch', 1, 1, 10, NULL, 4, GETDATE(), GETDATE()),

    ('PROD004', 'Sony Bravia X90L', 
     'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6544/6544734_sd.jpg',
     1400.00, 25, '4K LED TV with HDR', 'Full Array Local Dimming', '65 inch', 1, 1, 8, NULL, 4, GETDATE(), GETDATE()),

    -- Washing Machines (CategoryId = 5)
    ('PROD005', 'Samsung Front Load Washer', 
     'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6323/6323149cv3d.jpg',
     700.00, 15, 'Efficient washing with EcoBubble technology', '10kg Capacity', 'Inverter Motor', 1, 1, 5, NULL, 5, GETDATE(), GETDATE()),

    ('PROD006', 'LG Twin Wash', 
     'https://www.lg.com/au/images/WM/features/TWIN171216T_D_EDIT.jpg',
     800.00, 20, 'Dual Load with TurboWash Technology', '12kg Main, 2kg Mini', 'SmartThinQ App', 1, 1, 7, NULL, 5, GETDATE(), GETDATE()),

    -- Refrigerators (CategoryId = 6)
    ('PROD007', 'Samsung Family Hub', 
     'https://img.us.news.samsung.com/us/wp-content/uploads/2019/01/14103821/Family-Hub-2019_main.jpg',
     2500.00, 10, 'Smart Refrigerator with 21.5-inch touchscreen', 'Built-in cameras', '700L Capacity', 1, 1, 3, NULL, 6, GETDATE(), GETDATE()),

    ('PROD008', 'Hitachi French Door', 
     'https://media.signatureappliances.com.au/2/a/6/7/2a6795413aea42e5e0e84fc83e9c4bdf4541de88_Hitachi_RZX740RAXK_Fridge_Hero_2.jpg',
     2200.00, 12, 'Elegant design with advanced cooling', 'Dual Fan Cooling', '650L Capacity', 1, 1, 4, NULL, 6, GETDATE(), GETDATE()),

    -- Laptops (CategoryId = 7)
    ('PROD009', 'Dell XPS 15', 
     'https://tech.co.za/wp-content/uploads/2022/06/Dell-XSP-15-9520-v2.png',
     1800.00, 20, 'High-performance laptop with 15.6-inch 4K display', '4K Display, Core i7', '16GB RAM, 1TB SSD', 1, 1, 10, NULL, 7, GETDATE(), GETDATE()),

    ('PROD010', 'MacBook Pro 16"', 
     'https://pos.nvncdn.com/ac3ac6-57746/ps/MacBook-Pro-M4-16inch-2024-M4-Max-64GB-Ram-2TB-New-MDM-_20240109_PkDWIMpJVx.jpg?v=1760868990',
     2500.00, 15, 'Apple MacBook Pro with M1 Pro chip', 'Liquid Retina XDR Display', '16GB RAM, 1TB SSD', 1, 1, 12, NULL, 7, GETDATE(), GETDATE()),

    -- Kitchen Appliances (CategoryId = 8)
    ('PROD011', 'Philips Air Fryer', 
     'https://res.cloudinary.com/dqxh4rmi3/image/upload/v1737556385/supermarket/8388c612_f9cb_401a_b263_99a606b6112f_philps.jpg,https://m.media-amazon.com/images/I/71ZJSl4lN2L._AC_.jpg',
     200.00, 50, 'Healthy frying with minimal oil', 'Rapid Air Technology', '5.0L Capacity', 1, 1, 25, NULL, 8, GETDATE(), GETDATE()),

    ('PROD012', 'Bosch Dishwasher', 
     'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6360/6360644cv11d.jpg',
     900.00, 10, 'Energy-efficient dishwasher with quiet operation', 'EcoSilence Drive', '14 Place Settings', 1, 1, 20, NULL, 8, GETDATE(), GETDATE()),

    -- Gaming Consoles (CategoryId = 9)
    ('PROD013', 'PlayStation 5', 
     'https://pisces.bbystatic.com/image2/BestBuy_US/images/products/6523/6523225_bd.jpg',
     500.00, 30, 'Next-gen gaming console with immersive features', 'Ray Tracing, Ultra-fast SSD', '825GB SSD', 1, 1, 40, NULL, 9, GETDATE(), GETDATE()),

    ('PROD014', 'Xbox Series X', 
     'https://media.wired.com/photos/5fa5dc3dba670daaf8e97a8d/master/w_2560%2Cc_limit/games_gear_series-x.jpg',
     500.00, 35, 'Powerful gaming console with 4K gaming support', '12 Teraflops GPU', '1TB SSD', 1, 1, 35, NULL, 9, GETDATE(), GETDATE()),

    -- Microwaves (CategoryId = 10)
    ('PROD015', 'Panasonic Microwave Oven', 
     'https://www.panasonic.com/content/dam/pim/sg/en/NN/NN-GT3/NN-GT35NB/ast-1788963.png.pub.png',
     150.00, 25, 'Compact microwave with inverter technology', '1,000 Watts', '1.2 Cu. Ft.', 1, 1, 18, NULL, 10, GETDATE(), GETDATE()),

    ('PROD016', 'Samsung Smart Oven', 
     'https://farm8.staticflickr.com/7440/10816160456_3b5f82f233_b.jpg',
     250.00, 20, 'Combination microwave and convection oven', 'Hot Blast Technology', '1.4 Cu. Ft.', 1, 1, 16, NULL, 10, GETDATE(), GETDATE());
GO

-------------------------------------------------
-- 3️⃣ PROMOTIONS
-------------------------------------------------
INSERT INTO Promotions (InternalCode, [Name], [Start], [End], [Limit], Discount, PercentMax, [Percent], DiscountMax, Type, Status)
VALUES 
    ('PROMO001', '10% off for Electronics', '2025-01-01', '2025-01-31', 100, NULL, 10, 10, 200, 1, 1),
    ('PROMO002', 'Buy 2 get $100 off', '2025-01-01', '2025-01-31', 50, 100, NULL, NULL, NULL, 0, 1),
    ('PROMO003', '15% off on Televisions', '2025-02-01', '2025-02-28', 75, NULL, 15, 15, 300, 1, 1),
    ('PROMO004', 'Flat $200 off on Refrigerators', '2025-02-01', '2025-02-28', 50, 200, NULL, NULL, NULL, 0, 1),
    ('PROMO005', '5% off for Gaming Consoles', '2025-01-15', '2025-01-31', 100, NULL, 5, 5, 50, 1, 1),
    ('PROMO006', 'Buy 2 Kitchen Appliances, Save $150', '2025-02-01', '2025-02-28', 30, 150, NULL, NULL, NULL, 0, 1);
GO

-------------------------------------------------
-- 4️⃣ PROMOTION PRODUCT REQUIREMENTS
-------------------------------------------------
INSERT INTO PromotionProductRequirements ([Group], PromotionId, ProductId)
VALUES 
    (1, 1, 1), (1, 1, 2),     -- Electronics
    (2, 2, 5), (2, 2, 6),     -- Washing Machines
    (3, 3, 3), (3, 3, 4),     -- Televisions
    (4, 4, 7), (4, 4, 8),     -- Refrigerators
    (5, 5, 13), (5, 5, 14),   -- Gaming Consoles
    (6, 6, 11), (6, 6, 12);   -- Kitchen Appliances
GO

-------------------------------------------------
-- 5️⃣ CLEANUP UPDATE FLAGS
-------------------------------------------------
UPDATE Products SET IsDeleted = 0, Type = 0;
UPDATE Categories SET IsDeleted = 0;
GO
