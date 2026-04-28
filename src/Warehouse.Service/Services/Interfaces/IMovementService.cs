using Warehouse.Data.Entities;
using Warehouse.Service.DTOs;

namespace Warehouse.Service.Services.Interfaces;

public interface IMovementService
{
    Task<int> CreateAsync(int userId, MovementInput input);
    Task<StockMovement?> GetByIdAsync(int id);
    Task<IEnumerable<StockMovement>> GetHistoryAsync(DateTime? from, DateTime? to, string? type, int? productId);
}
