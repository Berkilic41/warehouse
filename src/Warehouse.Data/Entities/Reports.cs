namespace Warehouse.Data.Entities;

public class StockReportRow
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockThreshold { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsLowStock { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal StockValue { get; set; }
}

public class TopConsumedRow
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalConsumed { get; set; }
    public int MovementCount { get; set; }
}

public class DashboardStats
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int SupplierCount { get; set; }
    public int MovementsLast30Days { get; set; }
    public decimal TotalStockValue { get; set; }
}
