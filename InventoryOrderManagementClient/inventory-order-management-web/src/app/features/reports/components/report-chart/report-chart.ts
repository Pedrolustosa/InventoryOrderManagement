import { Component, ElementRef, OnDestroy, effect, input, signal, untracked, viewChild } from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { ReportChartModel, formatChartTooltip } from '../../models/report-chart.model';

let registered = false;

@Component({
  selector: 'app-report-chart',
  templateUrl: './report-chart.html',
})
export class ReportChart implements OnDestroy {
  readonly model = input.required<ReportChartModel>();
  readonly drawFailed = signal(false);
  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      const model = this.model();
      const canvas = this.canvas();
      untracked(() => this.draw(model, canvas?.nativeElement));
    });
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  height(): number {
    const model = this.model();
    return model.horizontal ? Math.max(240, model.labels.length * 36) : 288;
  }

  sliceColor(index: number): string {
    const series = this.model().series[0];
    return series?.colors?.[index] ?? series?.color ?? '#0f766e';
  }

  sliceValue(index: number): string {
    const value = this.model().series[0]?.data[index] ?? 0;
    return formatChartTooltip(value, this.model().format);
  }

  private draw(model: ReportChartModel, canvas?: HTMLCanvasElement): void {
    this.destroyChart();
    this.drawFailed.set(false);
    if (!canvas || model.labels.length === 0) {
      return;
    }

    if (!registered) {
      Chart.register(...registerables);
      registered = true;
    }

    try {
      const context = canvas.getContext('2d');
      if (!context) {
        this.drawFailed.set(true);
        return;
      }

      this.chart = new Chart(context, this.configuration(model));
    } catch {
      this.drawFailed.set(true);
    }
  }

  private configuration(model: ReportChartModel): ChartConfiguration {
    const horizontal = model.horizontal;
    return {
      type: model.type,
      data: {
        labels: model.labels,
        datasets: model.series.map((series) => ({
          label: series.label,
          data: series.data,
          backgroundColor: series.colors ?? series.color,
          borderColor: series.color,
          borderWidth: model.type === 'line' ? 2 : 0,
          tension: 0.2,
          pointRadius: 3,
        })),
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: false,
        indexAxis: horizontal ? 'y' : 'x',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (item) => {
                const parsed = horizontal ? item.parsed.x : item.parsed.y;
                const value = model.type === 'doughnut' ? Number(item.raw) : Number(parsed ?? 0);
                return `${item.dataset.label ?? item.label}: ${formatChartTooltip(value, model.format)}`;
              },
            },
          },
        },
        scales:
          model.type === 'doughnut'
            ? undefined
            : {
                x: {
                  beginAtZero: true,
                  title: { display: true, text: horizontal ? model.valueAxisLabel : model.categoryAxisLabel },
                  ticks: horizontal && model.format === 'currency' ? { callback: (value) => formatChartTooltip(Number(value), 'currency') } : undefined,
                },
                y: {
                  beginAtZero: true,
                  title: { display: true, text: horizontal ? model.categoryAxisLabel : model.valueAxisLabel },
                  ticks: !horizontal && model.format === 'currency' ? { callback: (value) => formatChartTooltip(Number(value), 'currency') } : undefined,
                },
              },
      },
    };
  }

  private destroyChart(): void {
    this.chart?.destroy();
    this.chart = null;
  }
}
