export interface OperationalDashboardSummary {
  generatedAtUtc: string;
  products: DashboardProductsSection;
  customers: DashboardPartySection;
  suppliers: DashboardPartySection;
  orders: DashboardOrdersSection;
  purchases: DashboardPurchasesSection;
  finance: DashboardFinanceSection | null;
  shipping: DashboardShippingSection;
  returns: DashboardReturnsSection;
  notifications: DashboardNotificationsSection | null;
}

export interface DashboardProductsSection {
  total: number;
  active: number;
  inactive: number;
  lowStock: number;
  outOfStock: number;
  totalUnitsInStock: number;
}

export interface DashboardPartySection {
  total: number;
  active: number;
}

export interface DashboardOrdersSection {
  total: number;
  draft: number;
  confirmed: number;
  shipped: number;
  delivered: number;
  cancelled: number;
  confirmedOrderValue: number;
  averageConfirmedOrderValue: number;
  confirmedInPeriod: number;
}

export interface DashboardPurchasesSection {
  total: number;
  draft: number;
  pending: number;
  completed: number;
  cancelled: number;
  commercialValue: number;
  sentInPeriod: number;
}

export interface DashboardFinanceSection {
  payablesPending: number;
  receivablesPending: number;
  overduePayables: number;
  overdueReceivables: number;
  payablesPendingAmount: number;
  receivablesPendingAmount: number;
  overduePayablesAmount: number;
  overdueReceivablesAmount: number;
}

export interface DashboardShippingSection {
  pending: number;
  inTransit: number;
  delivered: number;
  failed: number;
}

export interface DashboardReturnsSection {
  pending: number;
  awaitingInspection: number;
  completed: number;
  cancelled: number;
}

export interface DashboardNotificationsSection {
  unread: number;
}
