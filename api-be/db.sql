INSERT INTO Categories (InternalCode, Name, Icon, ParentId)
VALUES 
    ('CAT001', 'Electronics', 'electronics_icon.png', NULL), -- Root category
    ('CAT002', 'Home Appliances', 'home_appliances_icon.png', NULL), -- Root category
    ('CAT003', 'Smartphones', 'smartphones_icon.png', 26), -- Subcategory of Electronics
    ('CAT004', 'Televisions', 'televisions_icon.png', 26), -- Subcategory of Electronics
    ('CAT005', 'Washing Machines', 'washing_machines_icon.png', 27), -- Subcategory of Home Appliances
    ('CAT006', 'Refrigerators', 'refrigerators_icon.png', 27); -- Subcategory of Home Appliances


	INSERT INTO Categories (InternalCode, Name, Icon, ParentId)
VALUES 
    ('CAT007', 'Laptops', 'laptops_icon.png', 26), -- Subcategory of Electronics
    ('CAT008', 'Kitchen Appliances', 'kitchen_appliances_icon.png', 27), -- Subcategory of Home Appliances
    ('CAT009', 'Gaming Consoles', 'gaming_consoles_icon.png', 26), -- Subcategory of Electronics
    ('CAT010', 'Microwaves', 'microwaves_icon.png', 27); -- Subcategory of Home Appliances

	INSERT INTO Products (InternalCode, Name, Images, Price, Quantity, Describes, Feature, Specifications, Type, Status, Selling, ParentId, CategoryId, CreatedAt, UpdatedAt)
VALUES 
    -- Smartphones
    ('PROD001', 'iPhone 14 Pro Max', ' https://res.cloudinary.com/dqxh4rmi3/image/upload/v1737270570/supermarket/qvhfdr6irqlexgrvpdrg.jpg,
    https://res.cloudinary.com/dqxh4rmi3/image/upload/v1737624270/iphone-14-pro-max-gold-0-60ea017_vzbwaa.jpg', 1200.00, 50, 'Latest iPhone with A16 Bionic chip', '5G, 120Hz Display', '256GB, 6GB RAM', 1, 1, 20, NULL, 28, GETDATE(), GETDATE()),
    ('PROD002', 'Samsung Galaxy S23 Ultra', 'galaxy_s23.jpg', 1100.00, 40, 'Flagship Samsung smartphone with Snapdragon 8 Gen 2', '200MP Camera, S-Pen', '512GB, 12GB RAM', 1, 1, 15, NULL, 28, GETDATE(), GETDATE()),
    
    -- Televisions
    ('PROD003', 'LG OLED C3', 'lg_oled_c3.jpg', 1500.00, 30, '4K OLED TV with Dolby Vision', '120Hz Refresh Rate', '55 inch', 1, 1, 10, NULL, 29, GETDATE(), GETDATE()),
    ('PROD004', 'Sony Bravia X90L', 'sony_bravia.jpg', 1400.00, 25, '4K LED TV with HDR', 'Full Array Local Dimming', '65 inch', 1, 1, 8, NULL, 29, GETDATE(), GETDATE()),
    
    -- Washing Machines
    ('PROD005', 'Samsung Front Load Washer', 'samsung_washer.jpg', 700.00, 15, 'Efficient washing with EcoBubble technology', '10kg Capacity', 'Inverter Motor', 1, 1, 5, NULL, 30, GETDATE(), GETDATE()),
    ('PROD006', 'LG Twin Wash', 'lg_twinwash.jpg', 800.00, 20, 'Dual Load with TurboWash Technology', '12kg Main, 2kg Mini', 'SmartThinQ App', 1, 1, 7, NULL, 30, GETDATE(), GETDATE()),
    
    -- Refrigerators
    ('PROD007', 'Samsung Family Hub', 'samsung_fridge.jpg', 2500.00, 10, 'Smart Refrigerator with 21.5-inch touchscreen', 'Built-in cameras', '700L Capacity', 1, 1, 3, NULL, 31, GETDATE(), GETDATE()),
    ('PROD008', 'Hitachi French Door', 'hitachi_fridge.jpg', 2200.00, 12, 'Elegant design with advanced cooling', 'Dual Fan Cooling', '650L Capacity', 1, 1, 4, NULL, 31, GETDATE(), GETDATE());



	INSERT INTO Products (InternalCode, Name, Images, Price, Quantity, Describes, Feature, Specifications, Type, Status, Selling, ParentId, CategoryId, CreatedAt, UpdatedAt)
VALUES 
    -- Laptops
    ('PROD009', 'Dell XPS 15', 'dell_xps_15.jpg', 1800.00, 20, 'High-performance laptop with 15.6-inch 4K display', '4K Display, Core i7', '16GB RAM, 1TB SSD', 1, 1, 10, NULL, 32, GETDATE(), GETDATE()),
    ('PROD010', 'MacBook Pro 16"', 'macbook_pro.jpg', 2500.00, 15, 'Apple MacBook Pro with M1 Pro chip', 'Liquid Retina XDR Display', '16GB RAM, 1TB SSD', 1, 1, 12, NULL, 32, GETDATE(), GETDATE()),

    -- Kitchen Appliances
    ('PROD011', 'Philips Air Fryer', 'philips_air_fryer.jpg', 200.00, 50, 'Healthy frying with minimal oil', 'Rapid Air Technology', '5.0L Capacity', 1, 1, 25, NULL, 33, GETDATE(), GETDATE()),
    ('PROD012', 'Bosch Dishwasher', 'bosch_dishwasher.jpg', 900.00, 10, 'Energy-efficient dishwasher with quiet operation', 'EcoSilence Drive', '14 Place Settings', 1, 1, 20, NULL, 33, GETDATE(), GETDATE()),

    -- Gaming Consoles
    ('PROD013', 'PlayStation 5', 'ps5.jpg', 500.00, 30, 'Next-gen gaming console with immersive features', 'Ray Tracing, Ultra-fast SSD', '825GB SSD', 1, 1, 40, NULL, 34, GETDATE(), GETDATE()),
    ('PROD014', 'Xbox Series X', 'xbox_series_x.jpg', 500.00, 35, 'Powerful gaming console with 4K gaming support', '12 Teraflops GPU', '1TB SSD', 1, 1, 35, NULL, 34, GETDATE(), GETDATE()),

    -- Microwaves
    ('PROD015', 'Panasonic Microwave Oven', 'panasonic_microwave.jpg', 150.00, 25, 'Compact microwave with inverter technology', '1,000 Watts', '1.2 Cu. Ft.', 1, 1, 18, NULL, 35, GETDATE(), GETDATE()),
    ('PROD016', 'Samsung Smart Oven', 'samsung_oven.jpg', 250.00, 20, 'Combination microwave and convection oven', 'Hot Blast Technology', '1.4 Cu. Ft.', 1, 1, 16, NULL, 35, GETDATE(), GETDATE());


	INSERT INTO Promotions (InternalCode, [Name], [Start], [End] , Limit, Discount, PercentMax, [Percent], DiscountMax, Type, Status)
VALUES 
    ('PROMO001', '10% off for Electronics', '2025-01-01', '2025-01-31', 100, NULL, 10, 10, 200, 1, 1), -- Percent promotion
    ('PROMO002', 'Buy 2 get 100 off', '2025-01-01', '2025-01-31', 50, 100, NULL, NULL, NULL, 0, 1); -- Discount promotion

	INSERT INTO Promotions (InternalCode, [Name], [Start], [End], Limit, Discount, PercentMax, [Percent], DiscountMax, Type, Status)
VALUES 
    -- Promotion for TVs
    ('PROMO003', '15% off on Televisions', '2025-02-01', '2025-02-28', 75, NULL, 15, 15, 300, 1, 1), -- Percent promotion

    -- Promotion for Refrigerators
    ('PROMO004', 'Flat $200 off on Refrigerators', '2025-02-01', '2025-02-28', 50, 200, NULL, NULL, NULL, 0, 1), -- Discount promotion

    -- Promotion for Gaming Consoles
    ('PROMO005', '5% off for Gaming Consoles', '2025-01-15', '2025-01-31', 100, NULL, 5, 5, 50, 1, 1), -- Percent promotion

    -- Promotion for Kitchen Appliances
    ('PROMO006', 'Buy 2 Kitchen Appliances, Save $150', '2025-02-01', '2025-02-28', 30, 150, NULL, NULL, NULL, 0, 1); -- Discount promotion

	INSERT INTO PromotionProductRequirements ([Group], PromotionId, ProductId)
VALUES 
    -- Group 1: Electronics promotion (PROMO001)
    (1, 1, 1), -- iPhone 14 Pro Max
    (1, 1, 2), -- Samsung Galaxy S23 Ultra

    -- Group 2: Washing machines promotion (PROMO002)
    (2, 2, 5), -- Samsung Front Load Washer
    (2, 2, 6); -- LG Twin Wash

INSERT INTO PromotionProductRequirements ([Group], PromotionId, ProductId)
VALUES 
    -- Group 3: TVs promotion (PROMO003)
    (3, 3, 3), -- LG OLED C3
    (3, 3, 4), -- Sony Bravia X90L

    -- Group 4: Refrigerators promotion (PROMO004)
    (4, 4, 7), -- Samsung Family Hub
    (4, 4, 8), -- Hitachi French Door

    -- Group 5: Gaming Consoles promotion (PROMO005)
    (5, 5, 13), -- PlayStation 5
    (5, 5, 14), -- Xbox Series X

    -- Group 6: Kitchen Appliances promotion (PROMO006)
    (6, 6, 11), -- Philips Air Fryer
    (6, 6, 12); -- Bosch Dishwasher

	Update Products set IsDeleted = 'False' where 1=1
	Update Products set type = '0' where 1=1
	Update Categories set IsDeleted = 'False' where 1=1