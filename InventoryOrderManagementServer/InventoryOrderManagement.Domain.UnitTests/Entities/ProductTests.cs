using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveProduct()
    {
        var beforeUtc = DateTime.UtcNow;

        var product = Product.Create(
            " kb-001 ",
            " Mechanical Keyboard ",
            " RGB keyboard ",
            249.90m);

        var afterUtc = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("KB-001", product.Sku);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal("RGB keyboard", product.Description);
        Assert.Equal(249.90m, product.CurrentPrice);
        Assert.True(product.IsActive);
        Assert.InRange(product.CreatedAtUtc, beforeUtc.AddSeconds(-1), afterUtc.AddSeconds(1));
        Assert.Equal(DateTimeKind.Utc, product.CreatedAtUtc.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptySku_ThrowsDomainException(string? sku)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Product.Create(sku!, "Keyboard", null, 10m));

        Assert.Equal("SKU is required.", exception.Message);
    }

    [Fact]
    public void Create_WithSkuLongerThan50_ThrowsDomainException()
    {
        var sku = new string('A', Product.SkuMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() =>
            Product.Create(sku, "Keyboard", null, 10m));

        Assert.Equal($"SKU cannot exceed {Product.SkuMaxLength} characters.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Product.Create("SKU-1", name!, null, 10m));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNameLongerThan150_ThrowsDomainException()
    {
        var name = new string('N', Product.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() =>
            Product.Create("SKU-1", name, null, 10m));

        Assert.Equal($"Name cannot exceed {Product.NameMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithDescriptionLongerThan500_ThrowsDomainException()
    {
        var description = new string('D', Product.DescriptionMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() =>
            Product.Create("SKU-1", "Keyboard", description, 10m));

        Assert.Equal(
            $"Description cannot exceed {Product.DescriptionMaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Product.Create("SKU-1", "Keyboard", null, -0.01m));

        Assert.Equal("Current price cannot be negative.", exception.Message);
    }

    [Fact]
    public void Create_NormalizesSkuToUpperCaseWithoutOuterSpaces()
    {
        var product = Product.Create("  ab-99  ", "Mouse", null, 0m);

        Assert.Equal("AB-99", product.Sku);
    }

    [Fact]
    public void Create_WithWhitespaceDescription_StoresNull()
    {
        var product = Product.Create("SKU-1", "Keyboard", "   ", 10m);

        Assert.Null(product.Description);
    }

    [Fact]
    public void Create_AlwaysSetsIsActiveToTrue()
    {
        var product = Product.Create("SKU-1", "Keyboard", null, 10m);

        Assert.True(product.IsActive);
    }
}
