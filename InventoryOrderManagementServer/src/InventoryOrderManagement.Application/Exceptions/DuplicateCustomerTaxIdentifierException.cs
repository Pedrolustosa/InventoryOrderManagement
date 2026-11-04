namespace InventoryOrderManagement.Application.Exceptions;

public class DuplicateCustomerTaxIdentifierException : Exception
{
    public string TaxIdentifier { get; }

    public DuplicateCustomerTaxIdentifierException(string taxIdentifier)
        : base($"A customer with tax identifier '{taxIdentifier}' already exists.")
    {
        TaxIdentifier = taxIdentifier;
    }
}
