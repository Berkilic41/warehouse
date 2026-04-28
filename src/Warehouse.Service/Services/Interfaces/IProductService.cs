using Warehouse.Data.Entities;

namespace Warehouse.Service.Services.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null);
    Task<Product?> GetByIdAsync(int id);
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task SetActiveAsync(int id, bool active);
    Task<IEnumerable<Product>> GetLowStockAsync();
}
