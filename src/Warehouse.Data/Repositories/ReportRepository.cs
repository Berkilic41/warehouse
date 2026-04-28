using Microsoft.Data.SqlClient;
using System.Data;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly DbConnectionFactory _factory;
    public ReportRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<StockReportRow>> GetStockReportAsync(int? categoryId, bool lowStockOnly)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetStockReport", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LowStockOnly", lowStockOnly);

        var list = new List<StockReportRow>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new StockReportRow
            {
                Id = r.GetInt32(0), SKU = r.GetString(1), Name = r.GetString(2),
                Unit = r.GetString(3), CurrentStock = r.GetInt32(4), MinStockThreshold = r.GetInt32(5),
                CategoryId = r.GetInt32(6), CategoryName = r.GetString(7),
                IsLowStock = r.GetInt32(8) == 1,
                UnitPrice = r.IsDBNull(9) ? null : r.GetDecimal(9),
                StockValue = r.GetDecimal(10)
            });
        }
        return list;
    }

    public async Task<IEnumerable<TopConsumedRow>> GetTopConsumedAsync(DateTime? from, DateTime? to, int topN)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetTopConsumed", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ToDate", (object?)to ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TopN", topN);

        var list = new List<TopConsumedRow>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            // sp_GetTopConsumed stores Out quantities as negative; flip sign for display
            list.Add(new TopConsumedRow
            {
                Id = r.GetInt32(0), SKU = r.GetString(1), Name = r.GetString(2),
                Unit = r.GetString(3), CategoryName = r.GetString(4),
                TotalConsumed = Math.Abs(r.GetInt32(5)),
                MovementCount = r.GetInt32(6)
            });
        }
        return list.OrderByDescending(x => x.TotalConsumed).ToList();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var cmd = new SqlCommand("sp_GetDashboardStats", conn) { CommandType = CommandType.StoredProcedure };
        using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new DashboardStats
        {
            TotalProducts = r.GetInt32(0),
            LowStockCount = r.GetInt32(1),
            SupplierCount = r.GetInt32(2),
            MovementsLast30Days = r.GetInt32(3),
            TotalStockValue = r.GetDecimal(4)
        };
    }
}
