using Microsoft.Data.SqlClient;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly DbConnectionFactory _factory;
    public SupplierRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(@"
            SELECT s.Id, s.Name, s.ContactName, s.Email, s.Phone, s.Address, s.IsActive, s.CreatedAt,
                   (SELECT COUNT(*) FROM SupplierProducts sp WHERE sp.SupplierId = s.Id) AS PC
            FROM Suppliers s ORDER BY s.Name", conn);
        var list = new List<Supplier>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(MapWithCount(r));
        return list;
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand(
            "SELECT Id, Name, ContactName, Email, Phone, Address, IsActive, CreatedAt FROM Suppliers WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return Map(r);
    }

    public async Task<int> CreateAsync(Supplier s, IEnumerable<int> productIds)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO Suppliers (Name, ContactName, Email, Phone, Address)
                OUTPUT INSERTED.Id
                VALUES (@N, @C, @E, @P, @A)", conn, tx);
            cmd.Parameters.AddWithValue("@N", s.Name);
            cmd.Parameters.AddWithValue("@C", (object?)s.ContactName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@E", (object?)s.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@P", (object?)s.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@A", (object?)s.Address ?? DBNull.Value);
            var id = (int)(await cmd.ExecuteScalarAsync())!;

            foreach (var pid in productIds.Distinct())
            {
                using var ins = new SqlCommand("INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES (@S, @P)", conn, tx);
                ins.Parameters.AddWithValue("@S", id);
                ins.Parameters.AddWithValue("@P", pid);
                await ins.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
            return id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task UpdateAsync(Supplier s, IEnumerable<int> productIds)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(@"
                UPDATE Suppliers SET Name = @N, ContactName = @C, Email = @E, Phone = @P, Address = @A
                WHERE Id = @Id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@Id", s.Id);
                cmd.Parameters.AddWithValue("@N", s.Name);
                cmd.Parameters.AddWithValue("@C", (object?)s.ContactName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@E", (object?)s.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@P", (object?)s.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@A", (object?)s.Address ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            using (var del = new SqlCommand("DELETE FROM SupplierProducts WHERE SupplierId = @S", conn, tx))
            {
                del.Parameters.AddWithValue("@S", s.Id);
                await del.ExecuteNonQueryAsync();
            }
            foreach (var pid in productIds.Distinct())
            {
                using var ins = new SqlCommand("INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES (@S, @P)", conn, tx);
                ins.Parameters.AddWithValue("@S", s.Id);
                ins.Parameters.AddWithValue("@P", pid);
                await ins.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("UPDATE Suppliers SET IsActive = @A WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@A", active);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<int>> GetProductIdsAsync(int supplierId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("SELECT ProductId FROM SupplierProducts WHERE SupplierId = @S", conn);
        cmd.Parameters.AddWithValue("@S", supplierId);
        var list = new List<int>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(r.GetInt32(0));
        return list;
    }

    private static Supplier Map(SqlDataReader r) => new()
    {
        Id = r.GetInt32(0), Name = r.GetString(1),
        ContactName = r.IsDBNull(2) ? null : r.GetString(2),
        Email = r.IsDBNull(3) ? null : r.GetString(3),
        Phone = r.IsDBNull(4) ? null : r.GetString(4),
        Address = r.IsDBNull(5) ? null : r.GetString(5),
        IsActive = r.GetBoolean(6), CreatedAt = r.GetDateTime(7)
    };

    private static Supplier MapWithCount(SqlDataReader r)
    {
        var s = Map(r);
        s.ProductCount = r.GetInt32(8);
        return s;
    }
}
