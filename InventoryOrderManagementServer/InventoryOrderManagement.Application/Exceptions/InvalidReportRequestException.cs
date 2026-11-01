namespace InventoryOrderManagement.Application.Exceptions;

public sealed class InvalidReportRequestException : Exception
{
    public InvalidReportRequestException(string message)
        : base(message)
    {
    }

    public static InvalidReportRequestException StartNotBeforeEnd()
    {
        return new InvalidReportRequestException("The start date must be earlier than the end date.");
    }

    public static InvalidReportRequestException BoundsRequired()
    {
        return new InvalidReportRequestException(
            "A start date and an end date are required for this report.");
    }

    public static InvalidReportRequestException InvalidPage()
    {
        return new InvalidReportRequestException("Page must be at least 1 and page size must be between 1 and 100.");
    }

    public static InvalidReportRequestException InvalidLimit()
    {
        return new InvalidReportRequestException("Limit must be between 1 and 50.");
    }

    public static InvalidReportRequestException GroupByRequired()
    {
        return new InvalidReportRequestException("GroupBy is required for this report.");
    }
}
