using InventoryOrderManagement.Application.Abstractions.Reports;

namespace InventoryOrderManagement.Application.Reports;

internal static class CommercialValue
{
    public static (decimal Total, decimal Average, int Count) Summarize(IReadOnlyList<CommercialOrderAmount> orders)
    {
        decimal raw = 0;
        foreach (var order in orders)
        {
            raw += order.Subtotal;
        }

        return (MoneyMath.Round(raw), MoneyMath.Average(raw, orders.Count), orders.Count);
    }
}
