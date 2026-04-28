using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.Services.Interfaces;
using Warehouse.Web.ViewModels;

namespace Warehouse.Web.Controllers;

[Authorize]
public class SuppliersController : Controller
{
    private readonly ISupplierService _suppliers;
    private readonly IProductService _products;

    public SuppliersController(ISupplierService suppliers, IProductService products)
    {
        _suppliers = suppliers;
        _products = products;
    }

    public async Task<IActionResult> Index() => View(await _suppliers.GetAllAsync());

    [HttpGet, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create() => View("Form", new SupplierFormViewModel
    {
        AllProducts = await _products.GetAllAsync()
    });

    [HttpPost, Authorize(Roles = "Admin,Staff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllProducts = await _products.GetAllAsync();
            return View("Form", vm);
        }
        await _suppliers.CreateAsync(vm.ToEntity(), vm.ProductIds);
        TempData["Success"] = "Supplier created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _suppliers.GetByIdAsync(id);
        if (supplier is null) return NotFound();
        var ids = await _suppliers.GetProductIdsAsync(id);
        return View("Form", new SupplierFormViewModel
        {
            Id = supplier.Id, Name = supplier.Name, ContactName = supplier.ContactName,
            Email = supplier.Email, Phone = supplier.Phone, Address = supplier.Address,
            ProductIds = ids.ToList(),
            AllProducts = await _products.GetAllAsync()
        });
    }

    [HttpPost, Authorize(Roles = "Admin,Staff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllProducts = await _products.GetAllAsync();
            return View("Form", vm);
        }
        await _suppliers.UpdateAsync(vm.ToEntity(), vm.ProductIds);
        TempData["Success"] = "Supplier updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool active)
    {
        await _suppliers.SetActiveAsync(id, active);
        TempData["Success"] = active ? "Supplier activated." : "Supplier deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
