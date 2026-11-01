import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReportChartModel } from '../../models/report-chart.model';
import { ReportChart } from './report-chart';

const chartSpy = vi.hoisted(() => ({ configs: [] as { type: string; options?: { plugins?: { tooltip?: { callbacks?: { label?: (item: unknown) => string } } } } }[] }));

vi.mock('chart.js', () => ({
  Chart: class {
    static register(): void {}

    constructor(_canvas: unknown, config: (typeof chartSpy.configs)[number]) {
      chartSpy.configs.push(config);
    }

    destroy(): void {}
  },
  registerables: [],
}));

describe('ReportChart', () => {
  let fixture: ComponentFixture<ReportChart>;

  const currencyChart: ReportChartModel = {
    type: 'line',
    title: 'Confirmed order value',
    labels: ['01/11/2026'],
    series: [{ label: 'Confirmed order value', data: [999.3], color: '#0f766e' }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Confirmed order value (BRL)',
  };

  beforeEach(async () => {
    chartSpy.configs.length = 0;
    HTMLCanvasElement.prototype.getContext = vi.fn(() => ({})) as unknown as typeof HTMLCanvasElement.prototype.getContext;

    await TestBed.configureTestingModule({
      imports: [ReportChart],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportChart);
  });

  it('should draw a chart and format the tooltip in BRL', async () => {
    fixture.componentRef.setInput('model', currencyChart);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(chartSpy.configs).toHaveLength(1);
    expect(chartSpy.configs[0].type).toBe('line');
    const label = chartSpy.configs[0].options?.plugins?.tooltip?.callbacks?.label?.({
      parsed: { x: 0, y: 999.3 },
      raw: 999.3,
      dataset: { label: 'Confirmed order value' },
      label: '01/11/2026',
    });
    expect(label).toContain('999,30');
    expect(fixture.nativeElement.textContent).toContain('Confirmed order value');
    expect(fixture.nativeElement.querySelector('[aria-label="Legend"]')).not.toBeNull();
  });

  it('should show an empty state and replace the chart when the data changes', async () => {
    fixture.componentRef.setInput('model', { ...currencyChart, labels: [], series: [] });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No data for this chart.');
    expect(chartSpy.configs).toHaveLength(0);

    fixture.componentRef.setInput('model', currencyChart);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(chartSpy.configs).toHaveLength(1);
    expect(fixture.nativeElement.textContent).not.toContain('No data for this chart.');
  });
});
