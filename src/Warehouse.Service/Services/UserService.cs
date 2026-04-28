using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class UserService : IUserService
{
    private static readonly HashSet<string> ValidRoles = ["Admin", "Staff", "Viewer"];
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;

    public Task<IEnumerable<User>> GetAllAsync() => _repo.GetAllAsync();

    public Task UpdateRoleAsync(int id, string role)
    {
        if (!ValidRoles.Contains(role))
            throw new InvalidOperationException($"Invalid role. Must be one of: {string.Join(", ", ValidRoles)}");
        return _repo.UpdateRoleAsync(id, role);
    }

    public Task SetActiveAsync(int id, bool active) => _repo.SetActiveAsync(id, active);
}
