using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;
    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public Task<IEnumerable<Category>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Category?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<int> CreateAsync(Category c) => _repo.CreateAsync(c);
    public Task UpdateAsync(Category c) => _repo.UpdateAsync(c);
    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}
