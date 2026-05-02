using Moq;
using Warehouse.Data.Entities;
using Warehouse.Data.Repositories.Interfaces;
using Warehouse.Service.DTOs;
using Warehouse.Service.Services;
using Xunit;

namespace Warehouse.Tests.Services;

public class MovementServiceTests
{
    private readonly Mock<IMovementRepository> _movementRepo;
    private readonly Mock<IProductRepository>  _productRepo;
    private readonly MovementService           _service;

    public MovementServiceTests()
    {
        _movementRepo = new Mock<IMovementRepository>();
        _productRepo  = new Mock<IProductRepository>();
        _service      = new MovementService(_movementRepo.Object, _productRepo.Object);
    }

    private static MovementInput MakeInput(string type, params (int productId, int qty)[] items) => new()
    {
        MovementType = type,
        Items = items.Select(i => new MovementItemInput { ProductId = i.productId, Quantity = i.qty }).ToList()
    };

    private static Product MakeProduct(int id, string name, int stock) =>
        new() { Id = id, Name = name, SKU = $"SKU-{id}", CurrentStock = stock, IsActive = true };

    // ─── Validation ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Incoming")]
    [InlineData("Outgoing")]
    [InlineData("")]
    [InlineData("in")]
    public async Task CreateAsync_InvalidMovementType_ThrowsInvalidOperation(string type)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, MakeInput(type, (1, 5))));
        Assert.Contains("Invalid movement type", ex.Message);
        Assert.Contains("In", ex.Message);
        Assert.Contains("Out", ex.Message);
        Assert.Contains("Adjustment", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NoValidItems_ThrowsInvalidOperation()
    {
        var input = new MovementInput
        {
            MovementType = "In",
            Items = new List<MovementItemInput>
            {
                new() { ProductId = 0, Quantity = 5 },  // invalid ProductId
                new() { ProductId = 1, Quantity = 0 }   // invalid Quantity
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, input));
        Assert.Equal("At least one valid item is required.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EmptyItemList_ThrowsInvalidOperation()
    {
        var input = new MovementInput { MovementType = "In", Items = [] };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, input));
    }

    // ─── Out Movement Stock Validation ───────────────────────────────────────

    [Fact]
    public async Task CreateAsync_OutMovement_ProductNotFound_Throws()
    {
        _productRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, MakeInput("Out", (99, 5))));
        Assert.Contains("Product 99 not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_OutMovement_InsufficientStock_Throws()
    {
        _productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeProduct(1, "Widget", stock: 3));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, MakeInput("Out", (1, 10))));

        Assert.Contains("Insufficient stock", ex.Message);
        Assert.Contains("Widget", ex.Message);
        Assert.Contains("3", ex.Message);   // available
        Assert.Contains("10", ex.Message);  // requested
    }

    [Fact]
    public async Task CreateAsync_OutMovement_ExactStock_Succeeds()
    {
        _productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeProduct(1, "Widget", stock: 5));
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(100);

        var result = await _service.CreateAsync(1, MakeInput("Out", (1, 5)));

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task CreateAsync_OutMovement_SufficientStock_Succeeds()
    {
        _productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeProduct(1, "Widget", stock: 50));
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(42);

        var result = await _service.CreateAsync(1, MakeInput("Out", (1, 10)));

        Assert.Equal(42, result);
        _productRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OutMovement_NegativeQuantity_UsesAbsoluteValue()
    {
        // Quantity -5 means request for 5 units out
        _productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeProduct(1, "Widget", stock: 3));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, MakeInput("Out", (1, -5)))); // |−5| = 5 > 3
    }

    // ─── In/Adjustment — No Stock Check ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_InMovement_SkipsStockValidation()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);

        await _service.CreateAsync(1, MakeInput("In", (1, 100)));

        _productRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AdjustmentMovement_SkipsStockValidation()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(2);

        await _service.CreateAsync(1, MakeInput("Adjustment", (1, -3)));

        _productRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── Data Normalization ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhitespaceReference_StoresNull()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);
        var input = MakeInput("In", (1, 5));
        input.Reference = "   ";

        await _service.CreateAsync(1, input);

        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m => m.Reference == null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroSupplierId_StoresNull()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);
        var input = MakeInput("In", (1, 5));
        input.SupplierId = 0;

        await _service.CreateAsync(1, input);

        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m => m.SupplierId == null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidSupplierId_Stored()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);
        var input = MakeInput("In", (1, 5));
        input.SupplierId = 7;

        await _service.CreateAsync(1, input);

        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m => m.SupplierId == 7)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UserIdSetOnMovement()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);

        await _service.CreateAsync(userId: 42, MakeInput("In", (1, 5)));

        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m => m.UserId == 42)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MovementDateSetToUtcNow()
    {
        var before = DateTime.UtcNow;
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);

        await _service.CreateAsync(1, MakeInput("In", (1, 5)));

        var after = DateTime.UtcNow;
        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m =>
            m.MovementDate >= before && m.MovementDate <= after
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ItemsWithZeroQuantityFiltered()
    {
        _movementRepo.Setup(r => r.CreateAsync(It.IsAny<StockMovement>())).ReturnsAsync(1);
        var input = new MovementInput
        {
            MovementType = "In",
            Items = new List<MovementItemInput>
            {
                new() { ProductId = 1, Quantity = 10 },
                new() { ProductId = 2, Quantity = 0 },   // filtered
                new() { ProductId = 3, Quantity = 5 }
            }
        };

        await _service.CreateAsync(1, input);

        _movementRepo.Verify(r => r.CreateAsync(It.Is<StockMovement>(m =>
            m.Items.Count == 2 &&
            m.Items.All(i => i.Quantity != 0)
        )), Times.Once);
    }

    // ─── Multi-item Out Validation ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_OutMovement_MultiItem_FailsOnFirstInsufficientProduct()
    {
        _productRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeProduct(1, "OK Product", stock: 100));
        _productRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(MakeProduct(2, "Low Stock", stock: 1));

        var input = new MovementInput
        {
            MovementType = "Out",
            Items = new List<MovementItemInput>
            {
                new() { ProductId = 1, Quantity = 10 },
                new() { ProductId = 2, Quantity = 50 }  // insufficient
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(1, input));

        Assert.Contains("Low Stock", ex.Message);
        _movementRepo.Verify(r => r.CreateAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    // ─── GetByIdAsync / GetHistoryAsync ──────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var movement = new StockMovement { Id = 5, MovementType = "In" };
        _movementRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(movement);

        var result = await _service.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _movementRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((StockMovement?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_DelegatesToRepository()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to   = DateTime.UtcNow;
        _movementRepo.Setup(r => r.GetHistoryAsync(from, to, "In", null))
            .ReturnsAsync(new List<StockMovement>());

        var result = await _service.GetHistoryAsync(from, to, "In", null);

        Assert.NotNull(result);
        _movementRepo.Verify(r => r.GetHistoryAsync(from, to, "In", null), Times.Once);
    }
}
