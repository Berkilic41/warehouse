namespace Warehouse.Data.Entities;

public class Product
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = "pcs";
    public int MinStockThreshold { get; set; }
    public int CurrentStock { get; set; }
    public decimal? UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public bool IsLowStock => CurrentStock <= MinStockThreshold;
    public decimal StockValue => CurrentStock * (UnitPrice ?? 0);
}
