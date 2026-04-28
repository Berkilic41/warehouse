using Warehouse.Data.Entities;

namespace Warehouse.Service.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task UpdateRoleAsync(int id, string role);
    Task SetActiveAsync(int id, bool active);
}
