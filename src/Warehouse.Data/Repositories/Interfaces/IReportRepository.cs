using Warehouse.Data.Entities;

namespace Warehouse.Data.Repositories.Interfaces;

public interface IReportRepository
{
    Task<IEnumerable<StockReportRow>> GetStockReportAsync(int? categoryId, bool lowStockOnly);
    Task<IEnumerable<TopConsumedRow>> GetTopConsumedAsync(DateTime? from, DateTime? to, int topN);
    Task<DashboardStats> GetDashboardStatsAsync();
}
