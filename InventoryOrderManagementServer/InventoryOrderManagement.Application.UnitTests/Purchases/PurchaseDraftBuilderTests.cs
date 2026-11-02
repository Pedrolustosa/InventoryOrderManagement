using InventoryOrderManagement.Application.Abstractions.Persistence;
using InventoryOrderManagement.Application.Purchases;
using InventoryOrderManagement.Application.Purchases.Dtos;
using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;
using NSubstitute;

namespace InventoryOrderManagement.Application.UnitTests.Purchases;

public class PurchaseDraftBuilderTests
{
    private readonly ISupplierRepository _suppliers = Substitute.For<ISupplierRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly PurchaseDraftBuilder _builder;

    public PurchaseDraftBuilderTests()
    {
        _builder = new PurchaseDraftBuilder(_suppliers, _products);
    }

    [Fact]
    public async Task BuildItemsAsync_SameProductAndCost_ConsolidatesQuantities()
    {
        var product = Product.Create("sku-1", "Widget", null, 99m, Guid.NewGuid());
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var items = await _builder.BuildItemsAsync(
        [
            new PurchaseOrderItemRequest { ProductId = product.Id, OrderedQuantity = 8, UnitCost = 1.25m },
            new PurchaseOrderItemRequest { ProductId = product.Id, OrderedQuantity = 12, UnitCost = 1.250m }
        ],
            CancellationToken.None);

        var line = Assert.Single(items);
        Assert.Equal(20, line.OrderedQuantity);
        Assert.Equal(1.25m, line.UnitCost);
        Assert.Equal(25.00m, line.LineTotal);
    }

    [Fact]
    public async Task BuildItemsAsync_SameProductDifferentCost_ThrowsConflict()
    {
        var productId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<PurchaseOrderConflictException>(() =>
            _builder.BuildItemsAsync(
            [
                new PurchaseOrderItemRequest { ProductId = productId, OrderedQuantity = 1, UnitCost = 1.00m },
                new PurchaseOrderItemRequest { ProductId = productId, OrderedQuantity = 1, UnitCost = 1.50m }
            ],
                CancellationToken.None));

        Assert.Contains("different unit costs", exception.Message, StringComparison.OrdinalIgnoreCase);
        await _products.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
