import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthService } from './core/auth/services/auth.service';
import { permittedAuth } from './core/auth/testing/permitted-auth';
import { AppShell } from './features/shell/app-shell';
import { SIDEBAR_COLLAPSED_KEY } from './features/shell/navigation';

describe('App', () => {
  it('should create the app', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the application brand in the shell', async () => {
    localStorage.removeItem(SIDEBAR_COLLAPSED_KEY);
    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [provideRouter([]), { provide: AuthService, useValue: permittedAuth() }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppShell);
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Inventory Order Management');
  });
});
