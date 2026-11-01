using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Reports;

public static class ReportLimit
{
    public const int Default = 10;
    public const int Max = 50;

    public static int Normalize(int? limit)
    {
        var resolved = limit ?? Default;
        if (resolved < 1 || resolved > Max)
        {
            throw InvalidReportRequestException.InvalidLimit();
        }

        return resolved;
    }
}
