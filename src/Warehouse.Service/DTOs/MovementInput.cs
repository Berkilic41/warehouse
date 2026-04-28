namespace Warehouse.Service.DTOs;

public class MovementInput
{
    public string MovementType { get; set; } = "In";
    public string? Reference { get; set; }
    public string? Reason { get; set; }
    public int? SupplierId { get; set; }
    public string? Notes { get; set; }
    public List<MovementItemInput> Items { get; set; } = [];
}

public class MovementItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
