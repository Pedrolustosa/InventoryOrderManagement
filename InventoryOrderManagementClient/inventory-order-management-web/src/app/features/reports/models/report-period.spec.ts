import { resolveReportRange } from './report-period';
import { formatChartValue, formatPeriodKey, isGuid } from './report-format';
import { toCsv } from './csv';
import { bestSellersChart, orderTrendValueChart } from './report-charts';

describe('report period', () => {
  const now = new Date(2026, 10, 1, 15, 30, 0);

  it('should use the last seven local days as a half-open UTC interval', () => {
    const result = resolveReportRange('Last7Days', '', '', now);
    expect('range' in result).toBe(true);
    if (!('range' in result)) {
      return;
    }

    const start = new Date(result.range.startUtc);
    const end = new Date(result.range.endUtc);
    expect(start.getFullYear()).toBe(2026);
    expect(start.getMonth()).toBe(9);
    expect(start.getDate()).toBe(26);
    expect(start.getHours()).toBe(0);
    expect(end.getMonth()).toBe(10);
    expect(end.getDate()).toBe(2);
    expect(end.getHours()).toBe(0);
    expect(result.range.startLabel).toBe('26/10/2026');
    expect(result.range.endLabel).toBe('01/11/2026');
  });

  it('should cover the current month through the last local day', () => {
    const result = resolveReportRange('CurrentMonth', '', '', now);
    expect('range' in result && result.range.startLabel).toBe('01/11/2026');
    expect('range' in result && result.range.endLabel).toBe('30/11/2026');
  });

  it('should cover the previous month', () => {
    const result = resolveReportRange('PreviousMonth', '', '', now);
    expect('range' in result && result.range.startLabel).toBe('01/10/2026');
    expect('range' in result && result.range.endLabel).toBe('31/10/2026');
  });

  it('should cover the current year', () => {
    const result = resolveReportRange('CurrentYear', '', '', now);
    expect('range' in result && result.range.startLabel).toBe('01/01/2026');
    expect('range' in result && result.range.endLabel).toBe('31/12/2026');
  });

  it('should include a single custom day and reject an inverted period', () => {
    const sameDay = resolveReportRange('Custom', '2026-11-01', '2026-11-01', now);
    expect('range' in sameDay && sameDay.range.startLabel).toBe('01/11/2026');
    expect('range' in sameDay && sameDay.range.endLabel).toBe('01/11/2026');
    if ('range' in sameDay) {
      const end = new Date(sameDay.range.endUtc);
      expect(end.getDate()).toBe(2);
      expect(end.getHours()).toBe(0);
    }

    const inverted = resolveReportRange('Custom', '2026-11-02', '2026-11-01', now);
    expect('error' in inverted && inverted.error).toContain('later');
  });
});

describe('report formatting', () => {
  it('should keep period keys on the calendar date', () => {
    expect(formatPeriodKey('2026-11-01')).toBe('01/11/2026');
  });

  it('should format currency and integers in pt-BR', () => {
    expect(formatChartValue(999.3, 'currency')).toContain('999,30');
    expect(formatChartValue(12, 'integer')).toBe('12');
  });

  it('should recognize order identifiers', () => {
    expect(isGuid('79122c61-abb3-435d-b53d-f4506782e1ac')).toBe(true);
    expect(isGuid('ORD-1')).toBe(false);
  });

  it('should escape csv cells', () => {
    expect(toCsv([['a', 'b,c'], ['say "hi"']])).toBe('a,"b,c"\r\n"say ""hi"""');
  });

  it('should build chart series from historical report values', () => {
    const trend = orderTrendValueChart([
      { period: '2026-11-01', totalOrders: 3, totalConfirmedOrderValue: 999.3 },
    ]);
    expect(trend.format).toBe('currency');
    expect(trend.labels).toEqual(['01/11/2026']);
    expect(trend.series[0].data).toEqual([999.3]);
    expect(bestSellersChart([]).labels).toEqual([]);
  });
});
