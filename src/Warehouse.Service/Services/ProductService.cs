using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services.Interfaces;

namespace Warehouse.Service.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;

    public ProductService(IProductRepository products, ICategoryRepository categories)
    {
        _products = products;
        _categories = categories;
    }

    public Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null)
        => _products.GetAllAsync(search, categoryId);

    public Task<Product?> GetByIdAsync(int id) => _products.GetByIdAsync(id);

    public async Task<int> CreateAsync(Product product)
    {
        product.SKU = product.SKU.Trim().ToUpperInvariant();
        if (await _products.SkuExistsAsync(product.SKU))
            throw new InvalidOperationException($"SKU '{product.SKU}' is already in use.");
        if (!await _categories.ExistsAsync(product.CategoryId))
            throw new InvalidOperationException("Category does not exist.");
        return await _products.CreateAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        product.SKU = product.SKU.Trim().ToUpperInvariant();
        if (await _products.SkuExistsAsync(product.SKU, product.Id))
            throw new InvalidOperationException($"SKU '{product.SKU}' is already in use.");
        if (!await _categories.ExistsAsync(product.CategoryId))
            throw new InvalidOperationException("Category does not exist.");
        await _products.UpdateAsync(product);
    }

    public Task SetActiveAsync(int id, bool active) => _products.SetActiveAsync(id, active);
    public Task<IEnumerable<Product>> GetLowStockAsync() => _products.GetLowStockAsync();
}
