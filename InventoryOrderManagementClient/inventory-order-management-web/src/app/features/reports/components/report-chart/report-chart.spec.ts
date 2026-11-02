import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Chart, ChartConfiguration } from 'chart.js';
import { ReportChartModel } from '../../models/report-chart.model';
import { CHART_CONSTRUCTOR } from './chart-constructor.token';
import { ReportChart } from './report-chart';

describe('ReportChart', () => {
  let fixture: ComponentFixture<ReportChart>;
  const configs: ChartConfiguration[] = [];

  const MockChart = class {
    static register(): void {}

    constructor(_canvas: unknown, config: ChartConfiguration) {
      configs.push(config);
    }

    destroy(): void {}
  };

  const currencyChart: ReportChartModel = {
    type: 'line',
    title: 'REPORTS.CHARTS.CONFIRMED_VALUE',
    labels: ['01/11/2026'],
    series: [{ label: 'REPORTS.CHARTS.CONFIRMED_VALUE', data: [999.3], color: '#0f766e' }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_VALUE_BRL',
  };

  beforeEach(async () => {
    configs.length = 0;

    await TestBed.configureTestingModule({
      imports: [ReportChart],
      providers: [
        provideTranslateService(),
        { provide: CHART_CONSTRUCTOR, useValue: MockChart as unknown as typeof Chart },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportChart);
  });

  it('should draw a chart and format the tooltip in BRL', async () => {
    fixture.componentRef.setInput('model', currencyChart);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(configs).toHaveLength(1);
    expect(configs[0].type).toBe('line');
    const labelCallback = configs[0].options?.plugins?.tooltip?.callbacks?.label as
      | ((item: unknown) => string)
      | undefined;
    const label = labelCallback?.({
      parsed: { x: 0, y: 999.3 },
      raw: 999.3,
      dataset: { label: 'REPORTS.CHARTS.CONFIRMED_VALUE' },
      label: '01/11/2026',
    });
    expect(label).toContain('999,30');
    expect(fixture.nativeElement.textContent).toContain('REPORTS.CHARTS.CONFIRMED_VALUE');
    expect(fixture.nativeElement.querySelector('[aria-label="REPORTS.SHARED.LEGEND"]')).not.toBeNull();
  });

  it('should show an empty state and replace the chart when the data changes', async () => {
    fixture.componentRef.setInput('model', { ...currencyChart, labels: [], series: [] });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('REPORTS.SHARED.NO_CHART_DATA');
    expect(configs).toHaveLength(0);

    fixture.componentRef.setInput('model', currencyChart);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(configs).toHaveLength(1);
    expect(fixture.nativeElement.textContent).not.toContain('REPORTS.SHARED.NO_CHART_DATA');
  });
});
