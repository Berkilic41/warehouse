using Warehouse.Data.Entities;

namespace Warehouse.Data.Repositories.Interfaces;

public interface IMovementRepository
{
    Task<int> CreateAsync(StockMovement movement);
    Task<StockMovement?> GetByIdAsync(int id);
    Task<IEnumerable<StockMovement>> GetHistoryAsync(DateTime? from, DateTime? to, string? type, int? productId);
}
