namespace InventoryOrderManagement.Application.Exceptions;

public class DuplicateTaxIdentifierException : Exception
{
    public string TaxIdentifier { get; }

    public DuplicateTaxIdentifierException(string taxIdentifier)
        : base($"A supplier with tax identifier '{taxIdentifier}' already exists.")
    {
        TaxIdentifier = taxIdentifier;
    }
}
