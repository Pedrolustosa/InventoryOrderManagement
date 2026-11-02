import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, RouterOutlet, Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../core/auth/testing/permitted-auth';
import { HomeRedirect } from './app-shell';
import { Sidebar } from './sidebar';

@Component({
  imports: [Sidebar, RouterOutlet],
  template: '<app-sidebar /><router-outlet />',
})
class SidebarHost {}

const routes: Routes = [
  {
    path: '',
    component: SidebarHost,
    children: [
      { path: 'products', component: HomeRedirect },
      {
        path: 'orders',
        children: [
          { path: '', component: HomeRedirect },
          { path: 'new', component: HomeRedirect },
        ],
      },
      {
        path: 'reports',
        children: [
          { path: '', component: HomeRedirect },
          { path: 'inventory', component: HomeRedirect },
        ],
      },
    ],
  },
];

describe('Sidebar', () => {
  async function render(permissions?: string[], url = '/products'): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: AuthService,
          useValue: permittedAuth(permissions ? permittedUser({ permissions }) : permittedUser()),
        },
      ],
    });

    const harness = await RouterTestingHarness.create(url);
    return harness.fixture.nativeElement as HTMLElement;
  }

  function labels(root: HTMLElement): string[] {
    return [...root.querySelectorAll('nav[aria-label="Main"] a')].map((link) => link.getAttribute('aria-label') ?? '');
  }

  function groups(root: HTMLElement): string[] {
    return [...root.querySelectorAll('nav[aria-label="Main"] h2')].map((heading) => heading.textContent?.trim() ?? '');
  }

  it('shows every permitted group', async () => {
    const root = await render();

    expect(groups(root)).toEqual(['Catalog', 'Operations', 'Analysis', 'Administration']);
    expect(labels(root)).toEqual([
      'Products',
      'Categories',
      'Suppliers',
      'Customers',
      'Inventory',
      'Purchases',
      'Orders',
      'Reports',
      'Users',
      'Roles',
      'Permissions',
      'Security Audit',
    ]);
  });

  it('highlights the module for a nested route', async () => {
    const root = await render(undefined, '/orders/new');

    expect(root.querySelector('a[aria-label="Orders"]')?.getAttribute('aria-current')).toBe('page');
    expect(root.querySelector('a[aria-label="Products"]')?.getAttribute('aria-current')).toBeNull();
  });

  it('keeps Reports active on a report child route', async () => {
    const root = await render(undefined, '/reports/inventory');

    expect(root.querySelector('a[aria-label="Reports"]')?.getAttribute('aria-current')).toBe('page');
  });

  it('hides an item the user cannot view', async () => {
    const root = await render(['Products.View', 'Orders.View']);

    expect(labels(root)).toEqual(['Products', 'Orders']);
    expect(groups(root)).toEqual(['Catalog', 'Operations']);
  });

  it('hides a group when none of its items are allowed', async () => {
    const root = await render(['Reports.View']);

    expect(labels(root)).toEqual(['Reports']);
    expect(groups(root)).toEqual(['Analysis']);
  });
});
