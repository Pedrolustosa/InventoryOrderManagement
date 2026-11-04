using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Reports;

public static class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? DefaultPageSize;

        if (resolvedPage < 1 || resolvedPageSize < 1 || resolvedPageSize > MaxPageSize)
        {
            throw InvalidReportRequestException.InvalidPage();
        }

        return (resolvedPage, resolvedPageSize);
    }
}
