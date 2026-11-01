import { formatChartValue, formatPeriodKey } from './report-format';

export interface ReportChartSeries {
  label: string;
  data: number[];
  color: string;
  colors?: string[];
}

export interface ReportChartModel {
  type: 'bar' | 'line' | 'doughnut';
  title: string;
  labels: string[];
  series: ReportChartSeries[];
  format: 'currency' | 'integer';
  horizontal: boolean;
  categoryAxisLabel: string;
  valueAxisLabel: string;
}

export function formatChartTooltip(value: number, format: 'currency' | 'integer'): string {
  return formatChartValue(value, format);
}

export function emptyChart(title: string, format: 'currency' | 'integer'): ReportChartModel {
  return {
    type: 'bar',
    title,
    labels: [],
    series: [],
    format,
    horizontal: false,
    categoryAxisLabel: '',
    valueAxisLabel: '',
  };
}

export function periodLabels(keys: string[]): string[] {
  return keys.map((key) => formatPeriodKey(key));
}
