using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repo;
    public SupplierService(ISupplierRepository repo) => _repo = repo;

    public Task<IEnumerable<Supplier>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Supplier?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<int> CreateAsync(Supplier s, IEnumerable<int> ids) => _repo.CreateAsync(s, ids);
    public Task UpdateAsync(Supplier s, IEnumerable<int> ids) => _repo.UpdateAsync(s, ids);
    public Task SetActiveAsync(int id, bool active) => _repo.SetActiveAsync(id, active);
    public Task<IEnumerable<int>> GetProductIdsAsync(int id) => _repo.GetProductIdsAsync(id);
}
