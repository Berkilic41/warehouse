using Warehouse.Data.Entities;

namespace Warehouse.Data.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null, bool includeInactive = false);
    Task<Product?> GetByIdAsync(int id);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task SetActiveAsync(int id, bool active);
    Task<IEnumerable<Product>> GetLowStockAsync();
}
