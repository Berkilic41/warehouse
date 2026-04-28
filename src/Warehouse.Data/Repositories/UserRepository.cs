using Microsoft.Data.SqlClient;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _factory;
    public UserRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<User?> GetByEmailAsync(string email) => await SingleAsync("WHERE Email = @V", email);
    public async Task<User?> GetByIdAsync(int id) => await SingleAsync("WHERE Id = @V", id);

    public async Task<bool> ExistsByEmailAsync(string email) => await CountAsync("Email = @V", email) > 0;
    public async Task<bool> ExistsByUsernameAsync(string username) => await CountAsync("Username = @V", username) > 0;

    public async Task<int> CreateAsync(User user)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, DisplayName)
            OUTPUT INSERTED.Id
            VALUES (@U, @E, @H, @S, @R, @D)", conn);
        cmd.Parameters.AddWithValue("@U", user.Username);
        cmd.Parameters.AddWithValue("@E", user.Email);
        cmd.Parameters.AddWithValue("@H", user.PasswordHash);
        cmd.Parameters.AddWithValue("@S", user.PasswordSalt);
        cmd.Parameters.AddWithValue("@R", user.Role);
        cmd.Parameters.AddWithValue("@D", (object?)user.DisplayName ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Id, Username, Email, PasswordHash, PasswordSalt, Role, DisplayName, IsActive, CreatedAt FROM Users ORDER BY CreatedAt DESC", conn);
        var list = new List<User>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task UpdateRoleAsync(int id, string role)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Users SET Role = @R WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@R", role);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Users SET IsActive = @A WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@A", active);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> CountAsync(string where, object value)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand($"SELECT COUNT(1) FROM Users WHERE {where}", conn);
        cmd.Parameters.AddWithValue("@V", value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<User?> SingleAsync(string where, object value)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand($"SELECT Id, Username, Email, PasswordHash, PasswordSalt, Role, DisplayName, IsActive, CreatedAt FROM Users {where}", conn);
        cmd.Parameters.AddWithValue("@V", value);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    private static User Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0), Username = r.GetString(1), Email = r.GetString(2),
        PasswordHash = r.GetString(3), PasswordSalt = r.GetString(4), Role = r.GetString(5),
        DisplayName = r.IsDBNull(6) ? null : r.GetString(6),
        IsActive = r.GetBoolean(7), CreatedAt = r.GetDateTime(8)
    };
}
