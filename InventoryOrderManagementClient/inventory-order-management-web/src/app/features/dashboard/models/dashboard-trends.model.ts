export type DashboardTrendGroupBy = 'Day' | 'Week' | 'Month';

export interface DashboardTrends {
  groupBy: string;
  items: DashboardTrendItem[];
}

export interface DashboardTrendItem {
  period: string;
  salesCount: number;
  salesAmount: number;
  purchasesCount: number;
  purchasesAmount: number;
}
