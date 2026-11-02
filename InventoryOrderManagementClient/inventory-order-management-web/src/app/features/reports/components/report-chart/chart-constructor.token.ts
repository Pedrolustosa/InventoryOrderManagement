import { InjectionToken } from '@angular/core';
import { Chart } from 'chart.js';

export type ChartConstructor = typeof Chart;

export const CHART_CONSTRUCTOR = new InjectionToken<ChartConstructor>('CHART_CONSTRUCTOR', {
  providedIn: 'root',
  factory: () => Chart,
});
