namespace InventoryOrderManagement.Application.Reports.Dtos;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalItems { get; init; }

    public required int TotalPages { get; init; }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = totalItems == 0 ? 0 : (totalItems + pageSize - 1) / pageSize;
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }
}
