using Warehouse.Data.Entities;
using Warehouse.Service.DTOs;

namespace Warehouse.Service.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<User?> GetByIdAsync(int id);
}
