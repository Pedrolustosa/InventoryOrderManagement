export const REPORT_PRESETS = [
  'Last7Days',
  'Last30Days',
  'CurrentMonth',
  'PreviousMonth',
  'CurrentYear',
  'Custom',
] as const;

export type ReportPreset = (typeof REPORT_PRESETS)[number];

export const REPORT_PRESET_OPTIONS: { id: ReportPreset; labelKey: string }[] = [
  { id: 'Last7Days', labelKey: 'REPORTS.FILTERS.LAST_7_DAYS' },
  { id: 'Last30Days', labelKey: 'REPORTS.FILTERS.LAST_30_DAYS' },
  { id: 'CurrentMonth', labelKey: 'REPORTS.FILTERS.CURRENT_MONTH' },
  { id: 'PreviousMonth', labelKey: 'REPORTS.FILTERS.PREVIOUS_MONTH' },
  { id: 'CurrentYear', labelKey: 'REPORTS.FILTERS.CURRENT_YEAR' },
  { id: 'Custom', labelKey: 'REPORTS.FILTERS.CUSTOM_PERIOD' },
];

export interface ReportRange {
  startUtc: string;
  endUtc: string;
  startLabel: string;
  endLabel: string;
}

export type ReportRangeResult = { range: ReportRange } | { error: string };

export function resolveReportRange(
  preset: ReportPreset,
  customStart: string,
  customEnd: string,
  now: Date,
): ReportRangeResult {
  if (preset === 'Custom') {
    return customRange(customStart, customEnd);
  }

  const today = startOfLocalDay(now);
  const bounds = presetBounds(preset, today);
  return { range: toRange(bounds.start, bounds.end) };
}

function presetBounds(preset: Exclude<ReportPreset, 'Custom'>, today: Date): { start: Date; end: Date } {
  switch (preset) {
    case 'Last7Days':
      return { start: addDays(today, -6), end: addDays(today, 1) };
    case 'Last30Days':
      return { start: addDays(today, -29), end: addDays(today, 1) };
    case 'CurrentMonth':
      return {
        start: new Date(today.getFullYear(), today.getMonth(), 1),
        end: new Date(today.getFullYear(), today.getMonth() + 1, 1),
      };
    case 'PreviousMonth':
      return {
        start: new Date(today.getFullYear(), today.getMonth() - 1, 1),
        end: new Date(today.getFullYear(), today.getMonth(), 1),
      };
    case 'CurrentYear':
      return {
        start: new Date(today.getFullYear(), 0, 1),
        end: new Date(today.getFullYear() + 1, 0, 1),
      };
  }
}

function customRange(customStart: string, customEnd: string): ReportRangeResult {
  const start = parseLocalDate(customStart);
  const endDay = parseLocalDate(customEnd);

  if (!start || !endDay) {
    return { error: 'REPORTS.FILTERS.CHOOSE_DATES' };
  }

  if (start.getTime() > endDay.getTime()) {
    return { error: 'REPORTS.FILTERS.PERIOD_ERROR' };
  }

  return { range: toRange(start, addDays(endDay, 1)) };
}

function toRange(start: Date, endExclusive: Date): ReportRange {
  return {
    startUtc: start.toISOString(),
    endUtc: endExclusive.toISOString(),
    startLabel: formatLocalDate(start),
    endLabel: formatLocalDate(addDays(endExclusive, -1)),
  };
}

function startOfLocalDay(value: Date): Date {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate());
}

function addDays(value: Date, days: number): Date {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate() + days);
}

function parseLocalDate(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);
  if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
    return null;
  }

  return date;
}

function formatLocalDate(value: Date): string {
  const day = String(value.getDate()).padStart(2, '0');
  const month = String(value.getMonth() + 1).padStart(2, '0');
  return `${day}/${month}/${value.getFullYear()}`;
}
