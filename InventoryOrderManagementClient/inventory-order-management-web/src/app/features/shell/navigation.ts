export type NavIconName =
  | 'box'
  | 'tag'
  | 'truck'
  | 'users'
  | 'archive'
  | 'bag'
  | 'clipboard'
  | 'chart'
  | 'user'
  | 'shield'
  | 'key'
  | 'scroll'
  | 'wallet'
  | 'bank'
  | 'swap'
  | 'receipt'
  | 'clock'
  | 'undo';

export interface NavigationItem {
  /** i18n key under NAVIGATION.* */
  labelKey: string;
  route: string;
  icon: NavIconName;
  permission: string;
}

export interface NavigationGroup {
  /** i18n key under NAVIGATION.* */
  labelKey: string;
  items: NavigationItem[];
}

export const NAVIGATION_GROUPS: readonly NavigationGroup[] = [
  {
    labelKey: 'NAVIGATION.CATALOG',
    items: [
      { labelKey: 'NAVIGATION.PRODUCTS', route: '/products', icon: 'box', permission: 'Products.View' },
      { labelKey: 'NAVIGATION.CATEGORIES', route: '/categories', icon: 'tag', permission: 'Categories.View' },
      { labelKey: 'NAVIGATION.SUPPLIERS', route: '/suppliers', icon: 'truck', permission: 'Suppliers.View' },
      { labelKey: 'NAVIGATION.CUSTOMERS', route: '/customers', icon: 'users', permission: 'Customers.View' },
    ],
  },
  {
    labelKey: 'NAVIGATION.OPERATIONS',
    items: [
      { labelKey: 'NAVIGATION.INVENTORY', route: '/inventory', icon: 'archive', permission: 'Inventory.View' },
      { labelKey: 'NAVIGATION.PURCHASES', route: '/purchases', icon: 'bag', permission: 'Purchases.View' },
      { labelKey: 'NAVIGATION.ORDERS', route: '/orders', icon: 'clipboard', permission: 'Orders.View' },
      { labelKey: 'NAVIGATION.SHIPPING', route: '/shipping', icon: 'truck', permission: 'Shipments.View' },
      { labelKey: 'NAVIGATION.RETURNS', route: '/returns', icon: 'undo', permission: 'Returns.View' },
      {
        labelKey: 'NAVIGATION.CUSTOMER_RETURNS',
        route: '/returns/customer',
        icon: 'undo',
        permission: 'Returns.View',
      },
      {
        labelKey: 'NAVIGATION.SUPPLIER_RETURNS',
        route: '/returns/supplier',
        icon: 'undo',
        permission: 'Returns.View',
      },
    ],
  },
  {
    labelKey: 'NAVIGATION.FINANCE',
    items: [
      {
        labelKey: 'NAVIGATION.FINANCE_DASHBOARD',
        route: '/finance/dashboard',
        icon: 'wallet',
        permission: 'Finance.Reports.View',
      },
      { labelKey: 'NAVIGATION.PAYABLES', route: '/finance/payables', icon: 'receipt', permission: 'Finance.View' },
      {
        labelKey: 'NAVIGATION.RECEIVABLES',
        route: '/finance/receivables',
        icon: 'receipt',
        permission: 'Finance.View',
      },
      {
        labelKey: 'NAVIGATION.CASH_ACCOUNTS',
        route: '/finance/cash-accounts',
        icon: 'bank',
        permission: 'Finance.CashAccounts.View',
      },
      {
        labelKey: 'NAVIGATION.CASH_FLOW',
        route: '/finance/cash-flow',
        icon: 'chart',
        permission: 'Finance.CashFlow.View',
      },
      {
        labelKey: 'NAVIGATION.CASH_MOVEMENTS',
        route: '/finance/cash-movements',
        icon: 'swap',
        permission: 'Finance.CashFlow.View',
      },
      {
        labelKey: 'NAVIGATION.SETTLEMENTS',
        route: '/finance/settlements',
        icon: 'receipt',
        permission: 'Finance.View',
      },
      {
        labelKey: 'NAVIGATION.AGING',
        route: '/finance/reports/aging',
        icon: 'clock',
        permission: 'Finance.Reports.View',
      },
    ],
  },
  {
    labelKey: 'NAVIGATION.ANALYSIS',
    items: [{ labelKey: 'NAVIGATION.REPORTS', route: '/reports', icon: 'chart', permission: 'Reports.View' }],
  },
  {
    labelKey: 'NAVIGATION.ADMINISTRATION',
    items: [
      { labelKey: 'NAVIGATION.USERS', route: '/users', icon: 'user', permission: 'Users.View' },
      { labelKey: 'NAVIGATION.ROLES', route: '/roles', icon: 'shield', permission: 'Roles.View' },
      { labelKey: 'NAVIGATION.PERMISSIONS', route: '/permissions', icon: 'key', permission: 'Roles.View' },
      {
        labelKey: 'NAVIGATION.SECURITY_AUDIT',
        route: '/audit',
        icon: 'scroll',
        permission: 'Security.Audit.View',
      },
    ],
  },
];

export const SIDEBAR_COLLAPSED_KEY = 'iom.nav.collapsed';
