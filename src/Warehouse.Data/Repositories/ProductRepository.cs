using Microsoft.Data.SqlClient;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DbConnectionFactory _factory;
    public ProductRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null, bool includeInactive = false)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var conditions = new List<string>();
        if (!includeInactive) conditions.Add("p.IsActive = 1");
        if (!string.IsNullOrWhiteSpace(search)) conditions.Add("(p.Name LIKE @S OR p.SKU LIKE @S)");
        if (categoryId.HasValue) conditions.Add("p.CategoryId = @C");
        var where = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        using var cmd = new SqlCommand($@"
            SELECT p.Id, p.SKU, p.Name, p.Description, p.CategoryId, c.Name,
                   p.Unit, p.MinStockThreshold, p.CurrentStock, p.UnitPrice, p.IsActive, p.CreatedAt
            FROM Products p INNER JOIN Categories c ON c.Id = p.CategoryId
            {where} ORDER BY p.Name", conn);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@S", $"%{search}%");
        if (categoryId.HasValue) cmd.Parameters.AddWithValue("@C", categoryId.Value);

        var list = new List<Product>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT p.Id, p.SKU, p.Name, p.Description, p.CategoryId, c.Name,
                   p.Unit, p.MinStockThreshold, p.CurrentStock, p.UnitPrice, p.IsActive, p.CreatedAt
            FROM Products p INNER JOIN Categories c ON c.Id = p.CategoryId
            WHERE p.Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var sql = "SELECT COUNT(1) FROM Products WHERE SKU = @S" + (excludeId.HasValue ? " AND Id <> @Id" : "");
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@S", sku);
        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@Id", excludeId.Value);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<int> CreateAsync(Product p)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            INSERT INTO Products (SKU, Name, Description, CategoryId, Unit, MinStockThreshold, CurrentStock, UnitPrice)
            OUTPUT INSERTED.Id
            VALUES (@SKU, @N, @D, @C, @U, @M, @S, @P)", conn);
        cmd.Parameters.AddWithValue("@SKU", p.SKU);
        cmd.Parameters.AddWithValue("@N", p.Name);
        cmd.Parameters.AddWithValue("@D", (object?)p.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", p.CategoryId);
        cmd.Parameters.AddWithValue("@U", p.Unit);
        cmd.Parameters.AddWithValue("@M", p.MinStockThreshold);
        cmd.Parameters.AddWithValue("@S", p.CurrentStock);
        cmd.Parameters.AddWithValue("@P", (object?)p.UnitPrice ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateAsync(Product p)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            UPDATE Products SET SKU = @SKU, Name = @N, Description = @D, CategoryId = @C,
                                 Unit = @U, MinStockThreshold = @M, UnitPrice = @P
            WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.Parameters.AddWithValue("@SKU", p.SKU);
        cmd.Parameters.AddWithValue("@N", p.Name);
        cmd.Parameters.AddWithValue("@D", (object?)p.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@C", p.CategoryId);
        cmd.Parameters.AddWithValue("@U", p.Unit);
        cmd.Parameters.AddWithValue("@M", p.MinStockThreshold);
        cmd.Parameters.AddWithValue("@P", (object?)p.UnitPrice ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Products SET IsActive = @A WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@A", active);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT p.Id, p.SKU, p.Name, p.Description, p.CategoryId, c.Name,
                   p.Unit, p.MinStockThreshold, p.CurrentStock, p.UnitPrice, p.IsActive, p.CreatedAt
            FROM Products p INNER JOIN Categories c ON c.Id = p.CategoryId
            WHERE p.IsActive = 1 AND p.CurrentStock <= p.MinStockThreshold
            ORDER BY p.CurrentStock ASC", conn);
        var list = new List<Product>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    private static Product Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0), SKU = r.GetString(1), Name = r.GetString(2),
        Description = r.IsDBNull(3) ? null : r.GetString(3),
        CategoryId = r.GetInt32(4), CategoryName = r.GetString(5),
        Unit = r.GetString(6), MinStockThreshold = r.GetInt32(7),
        CurrentStock = r.GetInt32(8),
        UnitPrice = r.IsDBNull(9) ? null : r.GetDecimal(9),
        IsActive = r.GetBoolean(10), CreatedAt = r.GetDateTime(11)
    };
}
