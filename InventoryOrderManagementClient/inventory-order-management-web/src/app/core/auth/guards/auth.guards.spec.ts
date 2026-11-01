import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot } from '@angular/router';
import { Component, signal } from '@angular/core';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { authGuard, passwordChangedGuard, permissionGuard } from './auth.guards';
import { AuthService } from '../services/auth.service';
import { permittedAuth, permittedUser } from '../testing/permitted-auth';

@Component({
  imports: [HasPermissionDirective],
  template: `<button *hasPermission="'Products.Create'">New Product</button>`,
})
class Host {}

describe('access guards and directive', () => {
  function auth(user = permittedUser()): AuthService {
    return permittedAuth(user);
  }

  it('sends an anonymous visitor to login', async () => {
    const service = auth();
    vi.spyOn(service, 'isAuthenticated').mockReturnValue(false);
    vi.spyOn(service, 'loadSession').mockResolvedValue(null);
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: service }],
    });

    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/products' } as RouterStateSnapshot),
    );
    expect(TestBed.inject(Router).serializeUrl(result as never)).toContain('/login');
  });

  it('blocks the shell until the temporary password changes', () => {
    const service = auth(permittedUser({ mustChangePassword: true }));
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: service }],
    });
    const result = TestBed.runInInjectionContext(() => passwordChangedGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
    expect(TestBed.inject(Router).serializeUrl(result as never)).toBe('/change-password');
  });

  it('allows a route when the permission is present and denies it otherwise', () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: auth(permittedUser({ permissions: ['Products.View'] })) }],
    });
    const allowed = TestBed.runInInjectionContext(() => permissionGuard('Products.View')({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
    const denied = TestBed.runInInjectionContext(() => permissionGuard('Users.View')({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
    expect(allowed).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(denied as never)).toBe('/access-denied');
  });

  it('renders the host content only while the permission is granted', async () => {
    const current = signal(permittedUser({ permissions: ['Products.Create'] }));
    const service = permittedAuth();
    Object.assign(service, { currentUser: current.asReadonly() });
    await TestBed.configureTestingModule({
      imports: [Host],
      providers: [{ provide: AuthService, useValue: service }],
    }).compileComponents();

    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('New Product');

    current.set(permittedUser({ permissions: [] }));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('New Product');
  });
});
