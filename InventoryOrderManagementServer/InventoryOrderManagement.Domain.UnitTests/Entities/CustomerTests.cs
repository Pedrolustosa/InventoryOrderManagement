using InventoryOrderManagement.Domain.Entities;
using InventoryOrderManagement.Domain.Enums;
using InventoryOrderManagement.Domain.Exceptions;
using InventoryOrderManagement.Domain.ValueObjects;

namespace InventoryOrderManagement.Domain.UnitTests.Entities;

public class CustomerTests
{
    private static CustomerAddress ValidAddress() =>
        CustomerAddress.Create(
            "50000-000",
            "Example Street",
            "100",
            "Apt 1",
            "Boa Viagem",
            "Recife",
            "PE",
            "Brazil");

    [Fact]
    public void Create_WithValidData_CreatesActiveCustomer()
    {
        var customer = Customer.Create(
            CustomerType.Individual,
            " John Smith ",
            " 123.456.789-00 ",
            " john@example.com ",
            " +55 81 99999-8888 ",
            ValidAddress());

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal(CustomerType.Individual, customer.CustomerType);
        Assert.Equal("John Smith", customer.Name);
        Assert.Equal("123.456.789-00", customer.TaxIdentifier);
        Assert.Equal("12345678900", customer.NormalizedTaxIdentifier);
        Assert.Equal("john@example.com", customer.Email);
        Assert.True(customer.IsActive);
        Assert.NotNull(customer.Address);
        Assert.Null(customer.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WithoutTaxIdentifier_AllowsNull()
    {
        var customer = Customer.Create(CustomerType.Company, "Acme", null, null, null, null);

        Assert.Null(customer.TaxIdentifier);
        Assert.Null(customer.NormalizedTaxIdentifier);
        Assert.Null(customer.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Customer.Create(CustomerType.Individual, name!, null, null, null, null));

        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithNameLongerThan150_ThrowsDomainException()
    {
        var name = new string('N', Customer.NameMaxLength + 1);
        var exception = Assert.Throws<DomainException>(() =>
            Customer.Create(CustomerType.Individual, name, null, null, null, null));

        Assert.Equal($"Name cannot exceed {Customer.NameMaxLength} characters.", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Customer.Create(CustomerType.Individual, "John", null, "bad-email", null, null));

        Assert.Equal("Email format is invalid.", exception.Message);
    }

    [Fact]
    public void NormalizeTaxIdentifierForComparison_StripsFormatting()
    {
        var normalized = Customer.NormalizeTaxIdentifierForComparison("123.456.789-00");
        Assert.Equal("12345678900", normalized);
    }

    [Fact]
    public void Create_WithIncompleteAddress_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create("50000-000", "", "100", null, null, "Recife", "PE", "Brazil"));

        Assert.Equal("Street is required when address is provided.", exception.Message);
    }

    [Fact]
    public void Update_ChangesFieldsAndUpdatedAt()
    {
        var customer = Customer.Create(CustomerType.Individual, "Old", "111", null, null, null);

        customer.Update(
            CustomerType.Company,
            " New Name ",
            "22.222.222/0001-22",
            "a@b.com",
            "123",
            ValidAddress());

        Assert.Equal(CustomerType.Company, customer.CustomerType);
        Assert.Equal("New Name", customer.Name);
        Assert.Equal("22222222000122", customer.NormalizedTaxIdentifier);
        Assert.NotNull(customer.Address);
        Assert.NotNull(customer.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var customer = Customer.Create(CustomerType.Individual, "John", null, null, null, null);

        customer.Deactivate();

        Assert.False(customer.IsActive);
        Assert.NotNull(customer.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var customer = Customer.Create(CustomerType.Individual, "John", null, null, null, null);
        customer.Deactivate();

        var exception = Assert.Throws<DomainException>(() => customer.Deactivate());
        Assert.Equal("Customer is already inactive.", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidCustomerType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Customer.Create((CustomerType)999, "John", null, null, null, null));

        Assert.Equal("Customer type is invalid.", exception.Message);
    }
}
