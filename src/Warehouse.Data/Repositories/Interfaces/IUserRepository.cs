using Warehouse.Data.Entities;

namespace Warehouse.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<int> CreateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync();
    Task UpdateRoleAsync(int id, string role);
    Task SetActiveAsync(int id, bool active);
}
