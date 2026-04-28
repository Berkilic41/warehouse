using Warehouse.Data.Entities;

namespace Warehouse.Service.Services.Interfaces;

public interface IReportService
{
    Task<IEnumerable<StockReportRow>> GetStockReportAsync(int? categoryId, bool lowStockOnly);
    Task<IEnumerable<TopConsumedRow>> GetTopConsumedAsync(DateTime? from, DateTime? to, int topN = 10);
    Task<DashboardStats> GetDashboardStatsAsync();
}
