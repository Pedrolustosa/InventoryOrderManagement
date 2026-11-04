namespace InventoryOrderManagement.Application.Reports;

internal static class ReportBuckets
{
    public static DateTime Align(DateTime utcInstant, ReportGroupBy groupBy)
    {
        var date = DateTime.SpecifyKind(utcInstant.Date, DateTimeKind.Utc);

        return groupBy switch
        {
            ReportGroupBy.Day => date,
            ReportGroupBy.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            ReportGroupBy.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => date
        };
    }

    public static IReadOnlyList<DateTime> Enumerate(DateTime startUtc, DateTime endUtcExclusive, ReportGroupBy groupBy)
    {
        var cursor = Align(startUtc, groupBy);
        var keys = new List<DateTime>();

        while (cursor < endUtcExclusive)
        {
            keys.Add(cursor);
            cursor = groupBy switch
            {
                ReportGroupBy.Week => cursor.AddDays(7),
                ReportGroupBy.Month => cursor.AddMonths(1),
                _ => cursor.AddDays(1)
            };
        }

        return keys;
    }

    public static string Key(DateTime bucketStart)
    {
        return bucketStart.ToString("yyyy-MM-dd");
    }
}
