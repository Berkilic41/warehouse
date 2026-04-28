using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _repo;
    public ReportService(IReportRepository repo) => _repo = repo;

    public Task<IEnumerable<StockReportRow>> GetStockReportAsync(int? categoryId, bool lowStockOnly)
        => _repo.GetStockReportAsync(categoryId, lowStockOnly);

    public Task<IEnumerable<TopConsumedRow>> GetTopConsumedAsync(DateTime? from, DateTime? to, int topN = 10)
        => _repo.GetTopConsumedAsync(from, to, topN);

    public Task<DashboardStats> GetDashboardStatsAsync() => _repo.GetDashboardStatsAsync();
}
