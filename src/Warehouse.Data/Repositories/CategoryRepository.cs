using Microsoft.Data.SqlClient;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DbConnectionFactory _factory;
    public CategoryRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT c.Id, c.Name, c.Description,
                   (SELECT COUNT(*) FROM Products p WHERE p.CategoryId = c.Id AND p.IsActive = 1) AS PC
            FROM Categories c ORDER BY c.Name", conn);
        var list = new List<Category>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Category {
                Id = r.GetInt32(0), Name = r.GetString(1),
                Description = r.IsDBNull(2) ? null : r.GetString(2),
                ProductCount = r.GetInt32(3)
            });
        return list;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT Id, Name, Description FROM Categories WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Category { Id = r.GetInt32(0), Name = r.GetString(1), Description = r.IsDBNull(2) ? null : r.GetString(2) };
    }

    public async Task<int> CreateAsync(Category c)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("INSERT INTO Categories (Name, Description) OUTPUT INSERTED.Id VALUES (@N, @D)", conn);
        cmd.Parameters.AddWithValue("@N", c.Name);
        cmd.Parameters.AddWithValue("@D", (object?)c.Description ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Category c)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Categories SET Name = @N, Description = @D WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", c.Id);
        cmd.Parameters.AddWithValue("@N", c.Name);
        cmd.Parameters.AddWithValue("@D", (object?)c.Description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("DELETE FROM Categories WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Categories WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }
}
