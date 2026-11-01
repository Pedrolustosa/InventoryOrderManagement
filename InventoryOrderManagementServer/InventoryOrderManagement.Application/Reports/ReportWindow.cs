using InventoryOrderManagement.Application.Exceptions;

namespace InventoryOrderManagement.Application.Reports;

/// <summary>
/// Half-open UTC window [StartUtc, EndUtc). Explicit dates replace a preset.
/// A missing bound means that side of the window is open.
/// </summary>
public readonly record struct ReportWindow(DateTime? StartUtc, DateTime? EndUtc)
{
    public bool HasBounds => StartUtc.HasValue && EndUtc.HasValue;

    public static ReportWindow Resolve(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        ReportPeriodPreset? period,
        DateTime utcNow)
    {
        DateTime? start = AsUtc(startDateUtc);
        DateTime? end = AsUtc(endDateUtc);

        if (start is null && end is null && period is not null)
        {
            (start, end) = PresetBounds(period.Value, utcNow);
        }

        if (start is not null && end is not null && start.Value >= end.Value)
        {
            throw InvalidReportRequestException.StartNotBeforeEnd();
        }

        return new ReportWindow(start, end);
    }

    public void EnsureBounds()
    {
        if (!HasBounds)
        {
            throw InvalidReportRequestException.BoundsRequired();
        }
    }

    private static (DateTime Start, DateTime End) PresetBounds(ReportPeriodPreset period, DateTime utcNow)
    {
        var today = UtcDate(utcNow);

        return period switch
        {
            ReportPeriodPreset.Last7Days => (today.AddDays(-6), today.AddDays(1)),
            ReportPeriodPreset.Last30Days => (today.AddDays(-29), today.AddDays(1)),
            ReportPeriodPreset.CurrentMonth => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            ReportPeriodPreset.PreviousMonth => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1),
                new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)),
            ReportPeriodPreset.CurrentYear => (new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(today.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            _ => throw new InvalidReportRequestException("The report period is invalid.")
        };
    }

    private static DateTime UtcDate(DateTime utcNow)
    {
        return DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var instant = value.Value;
        return instant.Kind switch
        {
            DateTimeKind.Utc => instant,
            DateTimeKind.Local => instant.ToUniversalTime(),
            _ => DateTime.SpecifyKind(instant, DateTimeKind.Utc)
        };
    }
}
