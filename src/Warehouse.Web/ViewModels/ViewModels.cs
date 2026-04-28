using System.ComponentModel.DataAnnotations;
using Warehouse.Data.Entities;

namespace Warehouse.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class DashboardViewModel
{
    public DashboardStats Stats { get; set; } = new();
    public IEnumerable<Product> LowStockItems { get; set; } = [];
    public IEnumerable<TopConsumedRow> TopConsumed { get; set; } = [];
}

public class ProductFormViewModel
{
    public int? Id { get; set; }
    [Required, MaxLength(50)] public string SKU { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Required] public int CategoryId { get; set; }
    [Required, MaxLength(20)] public string Unit { get; set; } = "pcs";
    [Range(0, int.MaxValue)] public int MinStockThreshold { get; set; }
    [Range(0, int.MaxValue)] public int CurrentStock { get; set; }
    [Range(0, double.MaxValue)] public decimal? UnitPrice { get; set; }

    public IEnumerable<Category> Categories { get; set; } = [];

    public Product ToEntity() => new()
    {
        Id = Id ?? 0,
        SKU = SKU,
        Name = Name,
        Description = Description,
        CategoryId = CategoryId,
        Unit = Unit,
        MinStockThreshold = MinStockThreshold,
        CurrentStock = CurrentStock,
        UnitPrice = UnitPrice
    };
}

public class SupplierFormViewModel
{
    public int? Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(150)] public string? ContactName { get; set; }
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    [MaxLength(50)] public string? Phone { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    public List<int> ProductIds { get; set; } = [];

    public IEnumerable<Product> AllProducts { get; set; } = [];

    public Supplier ToEntity() => new()
    {
        Id = Id ?? 0,
        Name = Name,
        ContactName = ContactName,
        Email = Email,
        Phone = Phone,
        Address = Address
    };
}

public class MovementFormViewModel
{
    [Required] public string MovementType { get; set; } = "In";
    [MaxLength(100)] public string? Reference { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    public int? SupplierId { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public List<MovementItemRow> Items { get; set; } = [new()];

    public IEnumerable<Product> AllProducts { get; set; } = [];
    public IEnumerable<Supplier> AllSuppliers { get; set; } = [];
}

public class MovementItemRow
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
}

public class StockReportViewModel
{
    public IEnumerable<StockReportRow> Rows { get; set; } = [];
    public IEnumerable<Category> Categories { get; set; } = [];
    public int? CategoryId { get; set; }
    public bool LowStockOnly { get; set; }
}

public class MovementHistoryViewModel
{
    public IEnumerable<StockMovement> Movements { get; set; } = [];
    public IEnumerable<Product> AllProducts { get; set; } = [];
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? MovementType { get; set; }
    public int? ProductId { get; set; }
}

public class TopConsumedViewModel
{
    public IEnumerable<TopConsumedRow> Rows { get; set; } = [];
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int TopN { get; set; } = 10;
}
