namespace InventoryOrderManagement.Application.Reports;

internal static class MoneyMath
{
    public static decimal Round(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal Average(decimal total, int count)
    {
        return count == 0 ? 0 : Round(total / count);
    }

    public static decimal Rate(int numerator, int denominator)
    {
        return denominator == 0 ? 0 : Round(numerator * 100m / denominator);
    }
}
