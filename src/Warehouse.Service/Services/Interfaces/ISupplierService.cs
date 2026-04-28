using Warehouse.Data.Entities;

namespace Warehouse.Service.Services.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<int> CreateAsync(Supplier supplier, IEnumerable<int> productIds);
    Task UpdateAsync(Supplier supplier, IEnumerable<int> productIds);
    Task SetActiveAsync(int id, bool active);
    Task<IEnumerable<int>> GetProductIdsAsync(int supplierId);
}
