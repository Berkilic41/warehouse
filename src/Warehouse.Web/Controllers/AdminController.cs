using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _users;
    private readonly ICategoryService _categories;

    public AdminController(IUserService users, ICategoryService categories)
    {
        _users = users;
        _categories = categories;
    }

    public async Task<IActionResult> Users() => View(await _users.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        try { await _users.UpdateRoleAsync(id, role); TempData["Success"] = "Role updated."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        await _users.SetActiveAsync(id, active);
        TempData["Success"] = active ? "User activated." : "User disabled.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Categories() => View(await _categories.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            await _categories.CreateAsync(new Warehouse.Data.Entities.Category { Name = name, Description = description });
            TempData["Success"] = "Category created.";
        }
        return RedirectToAction(nameof(Categories));
    }
}
