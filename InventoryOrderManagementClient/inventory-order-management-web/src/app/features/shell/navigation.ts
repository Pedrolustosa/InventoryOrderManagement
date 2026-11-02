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
  | 'scroll';

export interface NavigationItem {
  label: string;
  route: string;
  icon: NavIconName;
  permission: string;
}

export interface NavigationGroup {
  label: string;
  items: NavigationItem[];
}

export const NAVIGATION_GROUPS: readonly NavigationGroup[] = [
  {
    label: 'Catalog',
    items: [
      { label: 'Products', route: '/products', icon: 'box', permission: 'Products.View' },
      { label: 'Categories', route: '/categories', icon: 'tag', permission: 'Categories.View' },
      { label: 'Suppliers', route: '/suppliers', icon: 'truck', permission: 'Suppliers.View' },
      { label: 'Customers', route: '/customers', icon: 'users', permission: 'Customers.View' },
    ],
  },
  {
    label: 'Operations',
    items: [
      { label: 'Inventory', route: '/inventory', icon: 'archive', permission: 'Inventory.View' },
      { label: 'Purchases', route: '/purchases', icon: 'bag', permission: 'Purchases.View' },
      { label: 'Orders', route: '/orders', icon: 'clipboard', permission: 'Orders.View' },
    ],
  },
  {
    label: 'Analysis',
    items: [{ label: 'Reports', route: '/reports', icon: 'chart', permission: 'Reports.View' }],
  },
  {
    label: 'Administration',
    items: [
      { label: 'Users', route: '/users', icon: 'user', permission: 'Users.View' },
      { label: 'Roles', route: '/roles', icon: 'shield', permission: 'Roles.View' },
      { label: 'Permissions', route: '/permissions', icon: 'key', permission: 'Roles.View' },
      { label: 'Security Audit', route: '/audit', icon: 'scroll', permission: 'Security.Audit.View' },
    ],
  },
];

export const SIDEBAR_COLLAPSED_KEY = 'iom.nav.collapsed';
