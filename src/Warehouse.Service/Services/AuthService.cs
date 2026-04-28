using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.DTOs;
using Warehouse.Service.Helpers;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    public AuthService(IUserRepository users) => _users = users;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _users.GetByEmailAsync(email);
        if (user is null) return AuthResult.Fail("Invalid email or password.");
        if (!user.IsActive) return AuthResult.Fail("This account is disabled.");
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return AuthResult.Fail("Invalid email or password.");
        return AuthResult.Ok(user);
    }

    public Task<User?> GetByIdAsync(int id) => _users.GetByIdAsync(id);
}
