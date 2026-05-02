using Moq;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.Services;
using Xunit;

namespace Warehouse.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository>  _productRepo;
    private readonly Mock<ICategoryRepository> _categoryRepo;
    private readonly ProductService            _service;

    public ProductServiceTests()
    {
        _productRepo  = new Mock<IProductRepository>();
        _categoryRepo = new Mock<ICategoryRepository>();
        _service      = new ProductService(_productRepo.Object, _categoryRepo.Object);
    }

    private static Product MakeProduct(string sku = "TEST-001", int categoryId = 1) => new()
    {
        Id = 0, SKU = sku, Name = "Test Product", CategoryId = categoryId,
        Unit = "pcs", CurrentStock = 10, MinStockThreshold = 2, IsActive = true
    };

    // ─── SKU Normalization ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SkuNormalizedToUpperCase()
    {
        var product = MakeProduct(sku: "  widget-x1  ");
        _productRepo.Setup(r => r.SkuExistsAsync("WIDGET-X1", null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync(1);

        await _service.CreateAsync(product);

        Assert.Equal("WIDGET-X1", product.SKU);
    }

    [Fact]
    public async Task UpdateAsync_SkuNormalizedToUpperCase()
    {
        var product = MakeProduct(sku: "widget-x1");
        product.Id = 5;
        _productRepo.Setup(r => r.SkuExistsAsync("WIDGET-X1", 5)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(product);

        Assert.Equal("WIDGET-X1", product.SKU);
    }

    // ─── CreateAsync Validation ───────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DuplicateSku_ThrowsInvalidOperation()
    {
        var product = MakeProduct("EXISTING-SKU");
        _productRepo.Setup(r => r.SkuExistsAsync("EXISTING-SKU", null)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(product));
        Assert.Contains("EXISTING-SKU", ex.Message);
        Assert.Contains("already in use", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsInvalidOperation()
    {
        var product = MakeProduct("NEW-SKU", categoryId: 99);
        _productRepo.Setup(r => r.SkuExistsAsync("NEW-SKU", null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(product));
        Assert.Contains("Category does not exist", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ValidProduct_ReturnsNewId()
    {
        var product = MakeProduct("VALID-001");
        _productRepo.Setup(r => r.SkuExistsAsync("VALID-001", null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(r => r.CreateAsync(It.IsAny<Product>())).ReturnsAsync(42);

        var result = await _service.CreateAsync(product);

        Assert.Equal(42, result);
        _productRepo.Verify(r => r.CreateAsync(product), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSku_RepositoryCreateNeverCalled()
    {
        var product = MakeProduct("DUPE-SKU");
        _productRepo.Setup(r => r.SkuExistsAsync("DUPE-SKU", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(product));

        _productRepo.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
    }

    // ─── UpdateAsync Validation ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_SameProductKeepsItsSku_Succeeds()
    {
        // Product ID 5 can use its own SKU (excludeId = 5)
        var product = MakeProduct("MY-OWN-SKU");
        product.Id = 5;
        _productRepo.Setup(r => r.SkuExistsAsync("MY-OWN-SKU", 5)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(product);

        _productRepo.Verify(r => r.UpdateAsync(product), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SkuTakenByAnotherProduct_Throws()
    {
        var product = MakeProduct("TAKEN-SKU");
        product.Id = 5;
        _productRepo.Setup(r => r.SkuExistsAsync("TAKEN-SKU", 5)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(product));
        Assert.Contains("TAKEN-SKU", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_InvalidCategory_Throws()
    {
        var product = MakeProduct("VALID-SKU", categoryId: 999);
        product.Id = 3;
        _productRepo.Setup(r => r.SkuExistsAsync("VALID-SKU", 3)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(product));
        Assert.Contains("Category does not exist", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Valid_CallsRepositoryUpdate()
    {
        var product = MakeProduct("UPD-001");
        product.Id = 7;
        _productRepo.Setup(r => r.SkuExistsAsync("UPD-001", 7)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(product);

        _productRepo.Verify(r => r.UpdateAsync(product), Times.Once);
    }

    // ─── Read-only Methods ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        var products = new[] { MakeProduct("A"), MakeProduct("B") };
        _productRepo.Setup(r => r.GetAllAsync("widget", 2, false)).ReturnsAsync(products);

        var result = await _service.GetAllAsync("widget", 2);

        Assert.Equal(2, result.Count());
        _productRepo.Verify(r => r.GetAllAsync("widget", 2, false), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsProduct()
    {
        var product = MakeProduct();
        product.Id = 3;
        _productRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(product);

        var result = await _service.GetByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _productRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetActiveAsync_DelegatesToRepository()
    {
        _productRepo.Setup(r => r.SetActiveAsync(5, false)).Returns(Task.CompletedTask);

        await _service.SetActiveAsync(5, false);

        _productRepo.Verify(r => r.SetActiveAsync(5, false), Times.Once);
    }

    [Fact]
    public async Task GetLowStockAsync_DelegatesToRepository()
    {
        var lowStock = new[] { MakeProduct("LOW-A"), MakeProduct("LOW-B") };
        _productRepo.Setup(r => r.GetLowStockAsync()).ReturnsAsync(lowStock);

        var result = await _service.GetLowStockAsync();

        Assert.Equal(2, result.Count());
    }

    // ─── Product Entity Logic ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 5, true)]   // stock=0, threshold=5  → low
    [InlineData(5, 5, true)]   // stock=5, threshold=5  → low (equal = low)
    [InlineData(6, 5, false)]  // stock=6, threshold=5  → ok
    [InlineData(100, 0, false)] // threshold=0 → ok
    public void Product_IsLowStock_CorrectLogic(int currentStock, int threshold, bool expectedLow)
    {
        var product = new Product { CurrentStock = currentStock, MinStockThreshold = threshold };
        Assert.Equal(expectedLow, product.IsLowStock);
    }

    [Fact]
    public void Product_StockValue_CalculatedCorrectly()
    {
        var product = new Product { CurrentStock = 10, UnitPrice = 15.50m };
        Assert.Equal(155.00m, product.StockValue);
    }

    [Fact]
    public void Product_StockValue_NullUnitPrice_ReturnsZero()
    {
        var product = new Product { CurrentStock = 10, UnitPrice = null };
        Assert.Equal(0m, product.StockValue);
    }
}
