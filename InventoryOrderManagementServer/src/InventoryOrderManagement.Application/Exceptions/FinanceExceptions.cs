namespace InventoryOrderManagement.Application.Exceptions;

public sealed class FinancialTitleNotFoundException : Exception
{
    public FinancialTitleNotFoundException(Guid titleId)
        : base($"Financial title '{titleId}' was not found.")
    {
        TitleId = titleId;
    }

    public Guid TitleId { get; }
}

public sealed class FinancialInstallmentNotFoundException : Exception
{
    public FinancialInstallmentNotFoundException(Guid installmentId)
        : base($"Financial installment '{installmentId}' was not found.")
    {
        InstallmentId = installmentId;
    }

    public Guid InstallmentId { get; }
}

public sealed class CashAccountNotFoundException : Exception
{
    public CashAccountNotFoundException(Guid cashAccountId)
        : base($"Cash account '{cashAccountId}' was not found.")
    {
        CashAccountId = cashAccountId;
    }

    public Guid CashAccountId { get; }
}

public sealed class InactiveCashAccountException : Exception
{
    public InactiveCashAccountException(Guid cashAccountId)
        : base($"Cash account '{cashAccountId}' is inactive and cannot receive new settlements.")
    {
        CashAccountId = cashAccountId;
    }

    public Guid CashAccountId { get; }
}

public sealed class DuplicateFinancialDocumentException : Exception
{
    public DuplicateFinancialDocumentException(string documentNumber)
        : base($"Document '{documentNumber}' is already used for this counterparty.")
    {
        DocumentNumber = documentNumber;
    }

    public string DocumentNumber { get; }
}

public sealed class InactiveSupplierFinanceException : Exception
{
    public InactiveSupplierFinanceException(Guid supplierId)
        : base($"Supplier '{supplierId}' is inactive and cannot originate a payable.")
    {
        SupplierId = supplierId;
    }

    public Guid SupplierId { get; }
}

public sealed class InactiveCustomerFinanceException : Exception
{
    public InactiveCustomerFinanceException(Guid customerId)
        : base($"Customer '{customerId}' is inactive and cannot originate a receivable.")
    {
        CustomerId = customerId;
    }

    public Guid CustomerId { get; }
}
