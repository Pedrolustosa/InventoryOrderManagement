namespace InventoryOrderManagement.Application.Finance;

public sealed class FinanceOptions
{
    public const string SectionName = "Finance";

    public const int MaxReceivableDays = 3650;

    /// <summary>
    /// Days between issue and due date for receivables created on shipment dispatch. Orders and customers
    /// carry no payment terms, so this single default applies.
    /// </summary>
    public int DefaultReceivableDays { get; set; } = 30;
}
