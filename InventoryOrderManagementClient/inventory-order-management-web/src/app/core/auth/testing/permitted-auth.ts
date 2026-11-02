import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CurrentUser } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

const ALL_PERMISSIONS = [
  'Products.View',
  'Products.Create',
  'Categories.View',
  'Categories.Create',
  'Categories.Update',
  'Categories.Deactivate',
  'Suppliers.View',
  'Suppliers.Create',
  'Suppliers.Update',
  'Suppliers.Deactivate',
  'Customers.View',
  'Customers.Create',
  'Customers.Update',
  'Customers.Deactivate',
  'Inventory.View',
  'Inventory.Receive',
  'Inventory.Exit',
  'Inventory.Adjust',
  'Inventory.MinimumStock.Update',
  'Orders.View',
  'Orders.Create',
  'Orders.UpdateDraft',
  'Orders.Confirm',
  'Orders.Ship',
  'Orders.Deliver',
  'Orders.Cancel',
  'Purchases.View',
  'Purchases.Create',
  'Purchases.UpdateDraft',
  'Purchases.Send',
  'Purchases.Receive',
  'Purchases.Cancel',
  'Reports.View',
  'Finance.View',
  'Finance.Payables.Create',
  'Finance.Payables.Update',
  'Finance.Payables.Cancel',
  'Finance.Payables.Settle',
  'Finance.Receivables.Create',
  'Finance.Receivables.Update',
  'Finance.Receivables.Cancel',
  'Finance.Receivables.Settle',
  'Finance.CashAccounts.View',
  'Finance.CashAccounts.Manage',
  'Finance.CashFlow.View',
  'Finance.Reports.View',
  'Users.View',
  'Users.Create',
  'Users.Update',
  'Users.Deactivate',
  'Roles.View',
  'Roles.Create',
  'Roles.Update',
  'Roles.Delete',
  'Security.Audit.View',
];

export function permittedUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: 'user-1',
    displayName: 'Test User',
    email: 'test@localhost',
    isActive: true,
    roles: ['Administrator'],
    permissions: ALL_PERMISSIONS,
    mustChangePassword: false,
    ...overrides,
  };
}

export function permittedAuth(user: CurrentUser = permittedUser()): AuthService {
  const current = signal<CurrentUser | null>(user);
  return {
    currentUser: current.asReadonly(),
    isLoading: signal(false),
    isAuthenticated: signal(user !== null).asReadonly(),
    mustChangePassword: signal(user?.mustChangePassword === true).asReadonly(),
    loadSession: () => Promise.resolve(current()),
    login: () => of(user),
    logout: () => {
      current.set(null);
      return of(undefined);
    },
    getCurrentUser: () => current(),
    changePassword: () => of(undefined),
    hasPermission: (permission: string) => current()?.permissions.includes(permission) === true,
    hasRole: (role: string) => current()?.roles.includes(role) === true,
    clearSession: () => current.set(null),
  } as unknown as AuthService;
}
