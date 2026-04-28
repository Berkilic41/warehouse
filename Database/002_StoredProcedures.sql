USE WarehouseDb;
GO

-- Current stock levels with threshold flag
CREATE OR ALTER PROCEDURE sp_GetStockReport
    @CategoryId INT = NULL,
    @LowStockOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.Id, p.SKU, p.Name, p.Unit, p.CurrentStock, p.MinStockThreshold,
        c.Id AS CategoryId, c.Name AS CategoryName,
        CASE WHEN p.CurrentStock <= p.MinStockThreshold THEN 1 ELSE 0 END AS IsLowStock,
        p.UnitPrice,
        (p.CurrentStock * ISNULL(p.UnitPrice, 0)) AS StockValue
    FROM Products p
    INNER JOIN Categories c ON c.Id = p.CategoryId
    WHERE p.IsActive = 1
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND (@LowStockOnly = 0 OR p.CurrentStock <= p.MinStockThreshold)
    ORDER BY IsLowStock DESC, p.Name;
END
GO

-- Movement history within a date range
CREATE OR ALTER PROCEDURE sp_GetMovementHistory
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL,
    @MovementType NVARCHAR(20) = NULL,
    @ProductId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        sm.Id, sm.MovementType, sm.Reference, sm.Reason, sm.MovementDate, sm.Notes,
        sm.SupplierId, s.Name AS SupplierName,
        sm.UserId, u.Username AS UserName,
        mi.Id AS ItemId, mi.ProductId, p.SKU, p.Name AS ProductName, p.Unit,
        mi.Quantity, mi.UnitPrice
    FROM StockMovements sm
    INNER JOIN MovementItems mi ON mi.MovementId = sm.Id
    INNER JOIN Products p ON p.Id = mi.ProductId
    LEFT JOIN Suppliers s ON s.Id = sm.SupplierId
    INNER JOIN Users u ON u.Id = sm.UserId
    WHERE (@FromDate IS NULL OR sm.MovementDate >= @FromDate)
      AND (@ToDate   IS NULL OR sm.MovementDate <  DATEADD(day, 1, @ToDate))
      AND (@MovementType IS NULL OR sm.MovementType = @MovementType)
      AND (@ProductId IS NULL OR mi.ProductId = @ProductId)
    ORDER BY sm.MovementDate DESC, sm.Id DESC, mi.Id;
END
GO

-- Top consumed (Out) products in date range
CREATE OR ALTER PROCEDURE sp_GetTopConsumed
    @FromDate DATETIME2 = NULL,
    @ToDate   DATETIME2 = NULL,
    @TopN     INT       = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@TopN)
        p.Id, p.SKU, p.Name, p.Unit,
        c.Name AS CategoryName,
        SUM(mi.Quantity) AS TotalConsumed,
        COUNT(DISTINCT sm.Id) AS MovementCount
    FROM MovementItems mi
    INNER JOIN StockMovements sm ON sm.Id = mi.MovementId
    INNER JOIN Products p ON p.Id = mi.ProductId
    INNER JOIN Categories c ON c.Id = p.CategoryId
    WHERE sm.MovementType = 'Out'
      AND (@FromDate IS NULL OR sm.MovementDate >= @FromDate)
      AND (@ToDate   IS NULL OR sm.MovementDate <  DATEADD(day, 1, @ToDate))
    GROUP BY p.Id, p.SKU, p.Name, p.Unit, c.Name
    ORDER BY TotalConsumed DESC;
END
GO

-- Dashboard stats
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM Products WHERE IsActive = 1) AS TotalProducts,
        (SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND CurrentStock <= MinStockThreshold) AS LowStockCount,
        (SELECT COUNT(*) FROM Suppliers WHERE IsActive = 1) AS SupplierCount,
        (SELECT COUNT(*) FROM StockMovements WHERE MovementDate >= DATEADD(day, -30, GETUTCDATE())) AS MovementsLast30Days,
        (SELECT ISNULL(SUM(CurrentStock * ISNULL(UnitPrice, 0)), 0) FROM Products WHERE IsActive = 1) AS TotalStockValue;
END
GO
