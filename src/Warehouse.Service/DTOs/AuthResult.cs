using Warehouse.Data.Entities;

namespace Warehouse.Service.DTOs;

public record AuthResult(bool Success, string? ErrorMessage = null, User? User = null)
{
    public static AuthResult Ok(User user) => new(true, null, user);
    public static AuthResult Fail(string message) => new(false, message);
}
