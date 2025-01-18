INSERT INTO Categories (InternalCode, Name, Icon, ParentId)
VALUES 
    ('CAT001', 'Electronics', 'electronics_icon.png', NULL), -- Root category
    ('CAT002', 'Home Appliances', 'home_appliances_icon.png', NULL), -- Root category
    ('CAT003', 'Smartphones', 'smartphones_icon.png', 26), -- Subcategory of Electronics
    ('CAT004', 'Televisions', 'televisions_icon.png', 26), -- Subcategory of Electronics
    ('CAT005', 'Washing Machines', 'washing_machines_icon.png', 27), -- Subcategory of Home Appliances
    ('CAT006', 'Refrigerators', 'refrigerators_icon.png', 27); -- Subcategory of Home Appliances


	INSERT INTO Products (InternalCode, Name, Images, Price, Quantity, Describes, Feature, Specifications, Type, Status, Selling, ParentId, CategoryId, CreatedAt, UpdatedAt)
VALUES 
    -- Smartphones
    ('PROD001', 'iPhone 14 Pro Max', 'iphone14.jpg', 1200.00, 50, 'Latest iPhone with A16 Bionic chip', '5G, 120Hz Display', '256GB, 6GB RAM', 1, 1, 20, NULL, 28, GETDATE(), GETDATE()),
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


	Update Products set IsDeleted = 'False' where 1=1
	Update Products set type = '0' where 1=1

		Update Categories set IsDeleted = 'False' where 1=1