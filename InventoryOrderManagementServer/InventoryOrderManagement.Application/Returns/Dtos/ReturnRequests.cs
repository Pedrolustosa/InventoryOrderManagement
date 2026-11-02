using InventoryOrderManagement.Domain.Enums;

namespace InventoryOrderManagement.Application.Returns.Dtos;

public sealed class CreateCustomerReturnRequest
{
    public Guid OrderId { get; init; }

    public ReturnReason Reason { get; init; }

    public string? ReasonDescription { get; init; }

    public string? Notes { get; init; }

    public ReturnFinancialTreatment FinancialTreatment { get; init; } = ReturnFinancialTreatment.None;

    public string? FinancialNotes { get; init; }

    public IReadOnlyList<CreateCustomerReturnItemRequest> Items { get; init; } = [];
}

public sealed class CreateCustomerReturnItemRequest
{
    public Guid OrderItemId { get; init; }

    public int RequestedQuantity { get; init; }
}

public sealed class CreateSupplierReturnRequest
{
    public Guid PurchaseOrderId { get; init; }

    public ReturnReason Reason { get; init; }

    public string? ReasonDescription { get; init; }

    public string? Notes { get; init; }

    public ReturnFinancialTreatment FinancialTreatment { get; init; } = ReturnFinancialTreatment.None;

    public string? FinancialNotes { get; init; }

    public IReadOnlyList<CreateSupplierReturnItemRequest> Items { get; init; } = [];
}

public sealed class CreateSupplierReturnItemRequest
{
    public Guid PurchaseOrderItemId { get; init; }

    public int RequestedQuantity { get; init; }
}

public sealed class UpdateReturnRequest
{
    public ReturnReason Reason { get; init; }

    public string? ReasonDescription { get; init; }

    public string? Notes { get; init; }

    public ReturnFinancialTreatment FinancialTreatment { get; init; } = ReturnFinancialTreatment.None;

    public string? FinancialNotes { get; init; }

    public IReadOnlyList<UpdateReturnItemRequest> Items { get; init; } = [];
}

public sealed class UpdateReturnItemRequest
{
    public Guid? OrderItemId { get; init; }

    public Guid? PurchaseOrderItemId { get; init; }

    public int RequestedQuantity { get; init; }
}

public sealed class ReceiveCustomerReturnRequest
{
    public string? Notes { get; init; }

    public IReadOnlyList<ReceiveCustomerReturnItemRequest> Items { get; init; } = [];
}

public sealed class ReceiveCustomerReturnItemRequest
{
    public Guid ReturnItemId { get; init; }

    public int ReceivedQuantity { get; init; }
}

public sealed class CompleteReturnInspectionRequest
{
    public string? Notes { get; init; }

    public IReadOnlyList<CompleteReturnInspectionItemRequest> Items { get; init; } = [];
}

public sealed class CompleteReturnInspectionItemRequest
{
    public Guid ReturnItemId { get; init; }

    public ReturnItemCondition Condition { get; init; }

    public int RestockedQuantity { get; init; }

    public int QuarantinedQuantity { get; init; }

    public int DiscardedQuantity { get; init; }

    public int RejectedQuantity { get; init; }

    public string? InspectionNotes { get; init; }
}

public sealed class AuthorizeSupplierReturnRequest
{
    public string? Notes { get; init; }

    public string? AuthorizationReference { get; init; }
}

public sealed class DispatchSupplierReturnRequest
{
    public string? Notes { get; init; }

    public string? DispatchReference { get; init; }
}

public sealed class CancelReturnRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class ReturnListQuery
{
    public ReturnType? Type { get; init; }

    public ReturnStatus? Status { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? SupplierId { get; init; }

    public Guid? OrderId { get; init; }

    public Guid? PurchaseOrderId { get; init; }

    public string? ReturnNumber { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
