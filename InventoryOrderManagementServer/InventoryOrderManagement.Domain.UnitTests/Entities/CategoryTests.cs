using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveCategory()
    {
        var category = Category.Create(" Electronics ", " Devices ");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Electronics", category.Name);
        Assert.Equal("ELECTRONICS", category.NormalizedName);
        Assert.Equal("Devices", category.Description);
        Assert.True(category.IsActive);
        Assert.Null(category.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var exception = Assert.Throws<DomainException>(() => Category.Create(name!, null));
        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNameLongerThan100_ThrowsDomainException()
    {
        var name = new string('N', Category.NameMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() => Category.Create(name, null));
        Assert.Equal($"Name cannot exceed {Category.NameMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithDescriptionLongerThan300_ThrowsDomainException()
    {
        var description = new string('D', Category.DescriptionMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() => Category.Create("Valid", description));
        Assert.Equal(
            $"Description cannot exceed {Category.DescriptionMaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Update_ChangesNameDescriptionAndUpdatedAt()
    {
        var category = Category.Create("Old", "Old desc");

        category.Update(" New Name ", " New desc ");

        Assert.Equal("New Name", category.Name);
        Assert.Equal("NEW NAME", category.NormalizedName);
        Assert.Equal("New desc", category.Description);
        Assert.NotNull(category.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var category = Category.Create("Electronics", null);

        category.Deactivate();

        Assert.False(category.IsActive);
        Assert.NotNull(category.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var category = Category.Create("Electronics", null);
        category.Deactivate();

        var exception = Assert.Throws<DomainException>(() => category.Deactivate());
        Assert.Equal("Category is already inactive.", exception.Message);
    }
}
