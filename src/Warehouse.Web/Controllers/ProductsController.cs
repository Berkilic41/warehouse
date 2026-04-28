using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.Services.Interfaces;
using Warehouse.Web.ViewModels;

namespace Warehouse.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;

    public ProductsController(IProductService products, ICategoryService categories)
    {
        _products = products;
        _categories = categories;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _categories.GetAllAsync();
        return View(await _products.GetAllAsync(search, categoryId));
    }

    [HttpGet, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create()
        => View("Form", new ProductFormViewModel { Categories = await _categories.GetAllAsync() });

    [HttpPost, Authorize(Roles = "Admin,Staff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
        try
        {
            await _products.CreateAsync(vm.ToEntity());
            TempData["Success"] = $"Product '{vm.SKU}' created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
    }

    [HttpGet, Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product is null) return NotFound();
        return View("Form", new ProductFormViewModel
        {
            Id = product.Id, SKU = product.SKU, Name = product.Name,
            Description = product.Description, CategoryId = product.CategoryId,
            Unit = product.Unit, MinStockThreshold = product.MinStockThreshold,
            CurrentStock = product.CurrentStock, UnitPrice = product.UnitPrice,
            Categories = await _categories.GetAllAsync()
        });
    }

    [HttpPost, Authorize(Roles = "Admin,Staff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
        try
        {
            await _products.UpdateAsync(vm.ToEntity());
            TempData["Success"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.Categories = await _categories.GetAllAsync();
            return View("Form", vm);
        }
    }

    [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool active)
    {
        await _products.SetActiveAsync(id, active);
        TempData["Success"] = active ? "Product activated." : "Product deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
