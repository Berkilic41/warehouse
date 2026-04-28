using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.DTOs;
using Warehouse.Service.Services.Interfaces;
using Warehouse.Web.ViewModels;

namespace Warehouse.Web.Controllers;

[Authorize]
public class MovementsController : Controller
{
    private readonly IMovementService _movements;
    private readonly IProductService _products;
    private readonly ISupplierService _suppliers;

    public MovementsController(IMovementService movements, IProductService products, ISupplierService suppliers)
    {
        _movements = movements;
        _products = products;
        _suppliers = suppliers;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? type, int? productId)
    {
        return View(new MovementHistoryViewModel
        {
            Movements = await _movements.GetHistoryAsync(from, to, type, productId),
            AllProducts = await _products.GetAllAsync(),
            FromDate = from, ToDate = to, MovementType = type, ProductId = productId
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await _movements.GetByIdAsync(id);
        if (m is null) return NotFound();
        return View(m);
    }

    [HttpGet, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(string? type)
    {
        return View(new MovementFormViewModel
        {
            MovementType = string.IsNullOrEmpty(type) ? "In" : type,
            AllProducts = await _products.GetAllAsync(),
            AllSuppliers = await _suppliers.GetAllAsync()
        });
    }

    [HttpPost, Authorize(Roles = "Admin,Staff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovementFormViewModel vm)
    {
        try
        {
            var input = new MovementInput
            {
                MovementType = vm.MovementType,
                Reference = vm.Reference,
                Reason = vm.Reason,
                SupplierId = vm.SupplierId,
                Notes = vm.Notes,
                Items = vm.Items.Select(i => new MovementItemInput
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var id = await _movements.CreateAsync(userId, input);
            TempData["Success"] = $"Movement #{id} recorded.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.AllProducts = await _products.GetAllAsync();
            vm.AllSuppliers = await _suppliers.GetAllAsync();
            if (!vm.Items.Any()) vm.Items = [new()];
            return View(vm);
        }
    }
}
