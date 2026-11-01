using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class SupplierTests
{
    [Fact]
    public void Create_WithValidData_CreatesActiveSupplier()
    {
        var supplier = Supplier.Create(
            " Tech Components Ltd. ",
            " 12.345.678/0001-90 ",
            " John Smith ",
            " contact@techcomponents.com ",
            " +55 81 3333-4444 ",
            " Primary supplier ");

        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal("Tech Components Ltd.", supplier.Name);
        Assert.Equal("12.345.678/0001-90", supplier.TaxIdentifier);
        Assert.Equal("12345678000190", supplier.NormalizedTaxIdentifier);
        Assert.Equal("John Smith", supplier.ContactName);
        Assert.Equal("contact@techcomponents.com", supplier.Email);
        Assert.Equal("+55 81 3333-4444", supplier.Phone);
        Assert.Equal("Primary supplier", supplier.Notes);
        Assert.True(supplier.IsActive);
        Assert.Null(supplier.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WithoutTaxIdentifier_AllowsNull()
    {
        var supplier = Supplier.Create("Local Shop", null, null, null, null, null);

        Assert.Null(supplier.TaxIdentifier);
        Assert.Null(supplier.NormalizedTaxIdentifier);
        Assert.True(supplier.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create(name!, null, null, null, null, null));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNameLongerThan150_ThrowsDomainException()
    {
        var name = new string('N', Supplier.NameMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create(name, null, null, null, null, null));

        Assert.Equal($"Name cannot exceed {Supplier.NameMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithContactNameLongerThan100_ThrowsDomainException()
    {
        var contact = new string('C', Supplier.ContactNameMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create("Valid", null, contact, null, null, null));

        Assert.Equal(
            $"Contact name cannot exceed {Supplier.ContactNameMaxLength} characters.",
            exception.Message);
    }

    [Fact]
    public void Create_WithEmailLongerThan254_ThrowsDomainException()
    {
        var email = $"{new string('a', 250)}@x.com";
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create("Valid", null, null, email, null, null));

        Assert.Equal($"Email cannot exceed {Supplier.EmailMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create("Valid", null, null, "not-an-email", null, null));

        Assert.Equal("Email format is invalid.", exception.Message);
    }

    [Fact]
    public void Create_WithPhoneLongerThan30_ThrowsDomainException()
    {
        var phone = new string('1', Supplier.PhoneMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create("Valid", null, null, null, phone, null));

        Assert.Equal($"Phone cannot exceed {Supplier.PhoneMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithNotesLongerThan500_ThrowsDomainException()
    {
        var notes = new string('N', Supplier.NotesMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() =>
            Supplier.Create("Valid", null, null, null, null, notes));

        Assert.Equal($"Notes cannot exceed {Supplier.NotesMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void NormalizeTaxIdentifierForComparison_StripsFormatting()
    {
        var normalized = Supplier.NormalizeTaxIdentifierForComparison("12.345.678/0001-90");
        Assert.Equal("12345678000190", normalized);
    }

    [Fact]
    public void Update_ChangesFieldsAndUpdatedAt()
    {
        var supplier = Supplier.Create("Old", "111", null, null, null, null);

        supplier.Update(
            " New Name ",
            "22.222.222/0001-22",
            "Contact",
            "a@b.com",
            "123",
            "Notes");

        Assert.Equal("New Name", supplier.Name);
        Assert.Equal("22.222.222/0001-22", supplier.TaxIdentifier);
        Assert.Equal("22222222000122", supplier.NormalizedTaxIdentifier);
        Assert.Equal("Contact", supplier.ContactName);
        Assert.Equal("a@b.com", supplier.Email);
        Assert.Equal("123", supplier.Phone);
        Assert.Equal("Notes", supplier.Notes);
        Assert.NotNull(supplier.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var supplier = Supplier.Create("Supplier", null, null, null, null, null);

        supplier.Deactivate();

        Assert.False(supplier.IsActive);
        Assert.NotNull(supplier.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var supplier = Supplier.Create("Supplier", null, null, null, null, null);
        supplier.Deactivate();

        var exception = Assert.Throws<DomainException>(() => supplier.Deactivate());
        Assert.Equal("Supplier is already inactive.", exception.Message);
    }
}
