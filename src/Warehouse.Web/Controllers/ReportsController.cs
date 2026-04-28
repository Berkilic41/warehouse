using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.Services.Interfaces;
using Warehouse.Web.ViewModels;

namespace Warehouse.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly ICategoryService _categories;

    public ReportsController(IReportService reports, ICategoryService categories)
    {
        _reports = reports;
        _categories = categories;
    }

    public async Task<IActionResult> Stock(int? categoryId, bool lowStockOnly = false)
    {
        return View(new StockReportViewModel
        {
            Rows = await _reports.GetStockReportAsync(categoryId, lowStockOnly),
            Categories = await _categories.GetAllAsync(),
            CategoryId = categoryId,
            LowStockOnly = lowStockOnly
        });
    }

    public async Task<IActionResult> TopConsumed(DateTime? from, DateTime? to, int topN = 10)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;
        return View(new TopConsumedViewModel
        {
            Rows = await _reports.GetTopConsumedAsync(from, to, topN),
            FromDate = from, ToDate = to, TopN = topN
        });
    }
}
