using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.Services.Interfaces;
using Warehouse.Web.ViewModels;

namespace Warehouse.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IReportService _reports;
    private readonly IProductService _products;

    public HomeController(IReportService reports, IProductService products)
    {
        _reports = reports;
        _products = products;
    }

    public async Task<IActionResult> Index()
    {
        return View(new DashboardViewModel
        {
            Stats = await _reports.GetDashboardStatsAsync(),
            LowStockItems = await _products.GetLowStockAsync(),
            TopConsumed = await _reports.GetTopConsumedAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 5)
        });
    }
}
