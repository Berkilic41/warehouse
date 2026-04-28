USE WarehouseDb;
GO

-- Default users. Password for ALL: "password123"
DECLARE @Hash NVARCHAR(512) = 'dNenHFzqIK7wTHP3rNRkWw/tqSBIttAjKbks5Tgt5KVD9Rhdnnwqsbtos28hfQ3dpOGciFK1kHO1PAYqGmSETw==';
DECLARE @Salt NVARCHAR(512) = 'y21nmTHP1Vwrtv6X7V+mLm30Xrh74VS6yVJTPjX6qGQO1qmlAUqyDPEODItndn+hacqZNPjczFgVk7qVBK8oOn3/QUfZgz0tuMJ5Jde9nBzQik2ZW8nEgIctMjS8ypPqqliYaB/CA2FJNmBqoOx7vypsuOmR6C8EyzIOst+sXQw=';

INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, DisplayName) VALUES
('admin',  'admin@warehouse.test',  @Hash, @Salt, 'Admin',  'Site Admin'),
('staff',  'staff@warehouse.test',  @Hash, @Salt, 'Staff',  'Warehouse Staff'),
('viewer', 'viewer@warehouse.test', @Hash, @Salt, 'Viewer', 'Read-only Viewer');

INSERT INTO Categories (Name, Description) VALUES
('Electronics',     'Electronic components and devices'),
('Office Supplies', 'Pens, paper, stationery'),
('Tools',           'Hand tools and power tools'),
('Cleaning',        'Cleaning supplies and chemicals'),
('Packaging',       'Boxes, tape, bubble wrap');

INSERT INTO Suppliers (Name, ContactName, Email, Phone, Address) VALUES
('TechParts Ltd',     'John Smith',   'sales@techparts.test', '+90 212 555 0101', 'Istanbul, TR'),
('OfficePro',         'Maria Lopez',  'maria@officepro.test', '+90 212 555 0102', 'Ankara, TR'),
('Industrial Supply', 'Ahmet Yılmaz', 'a.yilmaz@indsup.test', '+90 232 555 0103', 'Izmir, TR'),
('CleanCorp',         'Sarah Kim',    'orders@cleancorp.test','+90 212 555 0104', 'Bursa, TR');

INSERT INTO Products (SKU, Name, Description, CategoryId, Unit, MinStockThreshold, CurrentStock, UnitPrice) VALUES
('ELEC-001', 'USB-C Cable 1m',        'Braided USB-C charging cable',           1, 'pcs', 20, 150, 5.99),
('ELEC-002', 'Wireless Mouse',        'Bluetooth optical mouse',                1, 'pcs', 10, 8,  18.50),
('ELEC-003', 'HDMI Cable 2m',         '4K-capable HDMI cable',                  1, 'pcs', 15, 60, 9.75),
('OFF-001',  'Ballpoint Pen (Blue)',  'Box of 12',                              2, 'box', 5,  3,  4.20),
('OFF-002',  'A4 Copy Paper',         '500 sheets',                             2, 'pkg', 10, 45, 6.80),
('OFF-003',  'Sticky Notes Set',      '5 colors, 100 sheets each',              2, 'set', 8,  25, 3.95),
('TOOL-001', 'Screwdriver Set',       '8-piece precision set',                  3, 'set', 5,  12, 14.99),
('TOOL-002', 'Cordless Drill',        '18V with 2 batteries',                   3, 'pcs', 3,  2,  89.00),
('TOOL-003', 'Tape Measure 5m',       'Fiberglass tape measure',                3, 'pcs', 10, 30, 7.25),
('CLN-001',  'Disinfectant Spray',    '500 ml multi-surface',                   4, 'btl', 15, 50, 4.50),
('CLN-002',  'Microfiber Cloth Pack', '6-pack',                                 4, 'pkg', 10, 6,  8.90),
('PKG-001',  'Cardboard Box (M)',     '40×30×20 cm shipping box',               5, 'pcs', 50, 200, 1.25),
('PKG-002',  'Bubble Wrap Roll',      '50 cm × 10 m',                           5, 'roll', 5, 4,  12.00);

-- Connect suppliers to products they supply
INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES
(1,1),(1,2),(1,3),
(2,4),(2,5),(2,6),
(3,7),(3,8),(3,9),(3,12),
(4,10),(4,11),(4,12),(4,13);

-- Sample movements (history)
DECLARE @M1 INT, @M2 INT, @M3 INT, @M4 INT;

INSERT INTO StockMovements (MovementType, Reference, Reason, SupplierId, UserId, MovementDate, Notes)
VALUES ('In', 'PO-2026-0001', 'Initial stock from TechParts', 1, 2, DATEADD(day,-25,GETUTCDATE()), 'Q2 restock');
SET @M1 = SCOPE_IDENTITY();
INSERT INTO MovementItems (MovementId, ProductId, Quantity, UnitPrice) VALUES
(@M1, 1, 150, 5.99), (@M1, 2, 25, 18.50), (@M1, 3, 60, 9.75);

INSERT INTO StockMovements (MovementType, Reference, Reason, UserId, MovementDate, Notes)
VALUES ('Out', 'SO-2026-0042', 'Sale to ACME Corp', 2, DATEADD(day,-12,GETUTCDATE()), '');
SET @M2 = SCOPE_IDENTITY();
INSERT INTO MovementItems (MovementId, ProductId, Quantity, UnitPrice) VALUES
(@M2, 1, 30, NULL), (@M2, 3, 12, NULL), (@M2, 5, 5, NULL);

INSERT INTO StockMovements (MovementType, Reference, Reason, UserId, MovementDate, Notes)
VALUES ('Adjustment', 'ADJ-2026-0003', 'Inventory count correction', 1, DATEADD(day,-5,GETUTCDATE()), 'Recount after audit');
SET @M3 = SCOPE_IDENTITY();
INSERT INTO MovementItems (MovementId, ProductId, Quantity, UnitPrice) VALUES
(@M3, 4, -2, NULL), (@M3, 11, 1, NULL);

INSERT INTO StockMovements (MovementType, Reference, Reason, SupplierId, UserId, MovementDate, Notes)
VALUES ('In', 'PO-2026-0007', 'Office supplies restock', 2, 2, DATEADD(day,-2,GETUTCDATE()), '');
SET @M4 = SCOPE_IDENTITY();
INSERT INTO MovementItems (MovementId, ProductId, Quantity, UnitPrice) VALUES
(@M4, 5, 50, 6.80), (@M4, 6, 30, 3.95);
GO
