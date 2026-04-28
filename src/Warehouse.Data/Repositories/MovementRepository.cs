using Microsoft.Data.SqlClient;
using System.Data;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;

namespace Warehouse.Data.Repositories;

public class MovementRepository : IMovementRepository
{
    private readonly DbConnectionFactory _factory;
    public MovementRepository(DbConnectionFactory factory) => _factory = factory;

    public async Task<int> CreateAsync(StockMovement m)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        try
        {
            int movementId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO StockMovements (MovementType, Reference, Reason, SupplierId, UserId, MovementDate, Notes)
                OUTPUT INSERTED.Id
                VALUES (@T, @Ref, @Reason, @S, @U, @Date, @N)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@T", m.MovementType);
                cmd.Parameters.AddWithValue("@Ref", (object?)m.Reference ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Reason", (object?)m.Reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@S", (object?)m.SupplierId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@U", m.UserId);
                cmd.Parameters.AddWithValue("@Date", m.MovementDate);
                cmd.Parameters.AddWithValue("@N", (object?)m.Notes ?? DBNull.Value);
                movementId = (int)(await cmd.ExecuteScalarAsync())!;
            }

            foreach (var item in m.Items)
            {
                // For stock impact: In = +qty, Out = -qty, Adjustment = signed qty as entered
                int delta = m.MovementType switch
                {
                    "In" => Math.Abs(item.Quantity),
                    "Out" => -Math.Abs(item.Quantity),
                    _ => item.Quantity
                };
                int storedQty = m.MovementType == "Out" ? -Math.Abs(item.Quantity) : delta;

                using (var ins = new SqlCommand(
                    "INSERT INTO MovementItems (MovementId, ProductId, Quantity, UnitPrice) VALUES (@M, @P, @Q, @U)", conn, tx))
                {
                    ins.Parameters.AddWithValue("@M", movementId);
                    ins.Parameters.AddWithValue("@P", item.ProductId);
                    ins.Parameters.AddWithValue("@Q", storedQty);
                    ins.Parameters.AddWithValue("@U", (object?)item.UnitPrice ?? DBNull.Value);
                    await ins.ExecuteNonQueryAsync();
                }

                using var upd = new SqlCommand(
                    "UPDATE Products SET CurrentStock = CurrentStock + @D WHERE Id = @P", conn, tx);
                upd.Parameters.AddWithValue("@D", delta);
                upd.Parameters.AddWithValue("@P", item.ProductId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return movementId;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<StockMovement?> GetByIdAsync(int id)
    {
        var all = await GetHistoryInternalAsync(null, null, null, null, id);
        return all.FirstOrDefault();
    }

    public async Task<IEnumerable<StockMovement>> GetHistoryAsync(DateTime? from, DateTime? to, string? type, int? productId)
        => await GetHistoryInternalAsync(from, to, type, productId, null);

    private async Task<IEnumerable<StockMovement>> GetHistoryInternalAsync(DateTime? from, DateTime? to, string? type, int? productId, int? movementId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        SqlCommand cmd;
        if (movementId.HasValue)
        {
            cmd = new SqlCommand(@"
                SELECT sm.Id, sm.MovementType, sm.Reference, sm.Reason, sm.MovementDate, sm.Notes,
                       sm.SupplierId, s.Name, sm.UserId, u.Username,
                       mi.Id, mi.ProductId, p.SKU, p.Name, p.Unit, mi.Quantity, mi.UnitPrice
                FROM StockMovements sm
                INNER JOIN MovementItems mi ON mi.MovementId = sm.Id
                INNER JOIN Products p ON p.Id = mi.ProductId
                LEFT JOIN Suppliers s ON s.Id = sm.SupplierId
                INNER JOIN Users u ON u.Id = sm.UserId
                WHERE sm.Id = @Id ORDER BY mi.Id", conn);
            cmd.Parameters.AddWithValue("@Id", movementId.Value);
        }
        else
        {
            cmd = new SqlCommand("sp_GetMovementHistory", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FromDate", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ToDate",   (object?)to ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MovementType", (object?)type ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductId", (object?)productId ?? DBNull.Value);
        }

        var byId = new Dictionary<int, StockMovement>();
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var mid = r.GetInt32(0);
            if (!byId.TryGetValue(mid, out var m))
            {
                m = new StockMovement
                {
                    Id = mid, MovementType = r.GetString(1),
                    Reference = r.IsDBNull(2) ? null : r.GetString(2),
                    Reason = r.IsDBNull(3) ? null : r.GetString(3),
                    MovementDate = r.GetDateTime(4),
                    Notes = r.IsDBNull(5) ? null : r.GetString(5),
                    SupplierId = r.IsDBNull(6) ? null : r.GetInt32(6),
                    SupplierName = r.IsDBNull(7) ? null : r.GetString(7),
                    UserId = r.GetInt32(8), UserName = r.GetString(9)
                };
                byId[mid] = m;
            }
            m.Items.Add(new MovementItem
            {
                Id = r.GetInt32(10), MovementId = mid, ProductId = r.GetInt32(11),
                ProductSku = r.GetString(12), ProductName = r.GetString(13), Unit = r.GetString(14),
                Quantity = r.GetInt32(15),
                UnitPrice = r.IsDBNull(16) ? null : r.GetDecimal(16)
            });
        }
        cmd.Dispose();
        return byId.Values.OrderByDescending(x => x.MovementDate).ToList();
    }
}
