namespace Warehouse.Data.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public string MovementType { get; set; } = "In";
    public string? Reference { get; set; }
    public string? Reason { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Notes { get; set; }
    public List<MovementItem> Items { get; set; } = [];
}

public class MovementItem
{
    public int Id { get; set; }
    public int MovementId { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public string? Unit { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
