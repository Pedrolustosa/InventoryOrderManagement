using InventoryOrderManagement.Domain.Exceptions;

namespace InventoryOrderManagement.Domain.Finance;

public static class Money
{
    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal Require(decimal value, string name, bool allowZero = false, bool allowNegative = false)
    {
        if (value != Round(value))
        {
            throw new DomainException($"{name} must have at most two decimal places.");
        }

        if (allowNegative)
        {
            return value;
        }

        if (value < 0)
        {
            throw new DomainException($"{name} cannot be negative.");
        }

        if (!allowZero && value == 0)
        {
            throw new DomainException($"{name} must be greater than zero.");
        }

        return value;
    }
}
