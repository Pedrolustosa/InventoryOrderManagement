export const FINANCE_PRESETS = ['Today', 'Last7Days', 'Last30Days', 'CurrentMonth', 'PreviousMonth', 'Custom'] as const;
export type FinancePreset = (typeof FINANCE_PRESETS)[number];

export const FINANCE_PRESET_OPTIONS: { id: FinancePreset; labelKey: string }[] = [
  { id: 'Today', labelKey: 'FINANCE.FILTERS.TODAY' },
  { id: 'Last7Days', labelKey: 'FINANCE.FILTERS.LAST_7_DAYS' },
  { id: 'Last30Days', labelKey: 'FINANCE.FILTERS.LAST_30_DAYS' },
  { id: 'CurrentMonth', labelKey: 'FINANCE.FILTERS.CURRENT_MONTH' },
  { id: 'PreviousMonth', labelKey: 'FINANCE.FILTERS.PREVIOUS_MONTH' },
  { id: 'Custom', labelKey: 'FINANCE.FILTERS.CUSTOM' },
];

export interface FinanceRange {
  startUtc: string;
  endUtc: string;
  startLabel: string;
  endLabel: string;
}

export type FinanceRangeResult = { range: FinanceRange } | { error: string };

export function resolveFinanceRange(
  preset: FinancePreset,
  customStart: string,
  customEnd: string,
  now: Date,
): FinanceRangeResult {
  if (preset === 'Custom') {
    return customRange(customStart, customEnd);
  }

  const today = startOfLocalDay(now);
  const bounds = presetBounds(preset, today);
  return { range: toRange(bounds.start, bounds.end) };
}

function presetBounds(preset: Exclude<FinancePreset, 'Custom'>, today: Date): { start: Date; end: Date } {
  switch (preset) {
    case 'Today':
      return { start: today, end: addDays(today, 1) };
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
  }
}

function customRange(customStart: string, customEnd: string): FinanceRangeResult {
  const start = parseLocalDate(customStart);
  const endDay = parseLocalDate(customEnd);
  if (!start || !endDay) {
    return { error: 'FINANCE.FILTERS.CHOOSE_DATES' };
  }

  if (start.getTime() > endDay.getTime()) {
    return { error: 'FINANCE.FILTERS.PERIOD_ERROR' };
  }

  return { range: toRange(start, addDays(endDay, 1)) };
}

function toRange(start: Date, endExclusive: Date): FinanceRange {
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
