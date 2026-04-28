using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.DTOs;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class MovementService : IMovementService
{
    private readonly IMovementRepository _movements;
    private readonly IProductRepository _products;
    private static readonly HashSet<string> ValidTypes = ["In", "Out", "Adjustment"];

    public MovementService(IMovementRepository movements, IProductRepository products)
    {
        _movements = movements;
        _products = products;
    }

    public async Task<int> CreateAsync(int userId, MovementInput input)
    {
        if (!ValidTypes.Contains(input.MovementType))
            throw new InvalidOperationException($"Invalid movement type. Must be one of: {string.Join(", ", ValidTypes)}");

        var validItems = input.Items.Where(i => i.ProductId > 0 && i.Quantity != 0).ToList();
        if (!validItems.Any())
            throw new InvalidOperationException("At least one valid item is required.");

        // For Out movements, validate stock availability
        if (input.MovementType == "Out")
        {
            foreach (var item in validItems)
            {
                var product = await _products.GetByIdAsync(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");
                if (product.CurrentStock < Math.Abs(item.Quantity))
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. Available: {product.CurrentStock}, requested: {Math.Abs(item.Quantity)}.");
            }
        }

        var movement = new StockMovement
        {
            MovementType = input.MovementType,
            Reference = string.IsNullOrWhiteSpace(input.Reference) ? null : input.Reference.Trim(),
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(),
            SupplierId = input.SupplierId > 0 ? input.SupplierId : null,
            UserId = userId,
            MovementDate = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            Items = validItems.Select(i => new MovementItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        return await _movements.CreateAsync(movement);
    }

    public Task<StockMovement?> GetByIdAsync(int id) => _movements.GetByIdAsync(id);

    public Task<IEnumerable<StockMovement>> GetHistoryAsync(DateTime? from, DateTime? to, string? type, int? productId)
        => _movements.GetHistoryAsync(from, to, type, productId);
}
