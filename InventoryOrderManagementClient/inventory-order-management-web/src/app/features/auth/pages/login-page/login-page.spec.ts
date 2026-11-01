import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let auth: AuthService;

  beforeEach(async () => {
    auth = permittedAuth();
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
    fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
  });

  it('navigates to the first allowed module after a valid login', () => {
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    vi.spyOn(auth, 'login').mockReturnValue(of(permittedUser({ permissions: ['Orders.View'] })));

    fixture.componentInstance.form.setValue({ email: 'ada@localhost', password: 'Secret!Admin1' });
    fixture.componentInstance.submit();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/orders');
  });

  it('stays on the form and shows the API message when the password is wrong', () => {
    vi.spyOn(auth, 'login').mockReturnValue(throwError(() => new Error('The email or password is incorrect.')));
    fixture.componentInstance.form.setValue({ email: 'ada@localhost', password: 'wrong' });
    fixture.componentInstance.submit();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('The email or password is incorrect.');
  });

  it('sends a user who must change the password to the change page', () => {
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    vi.spyOn(auth, 'login').mockReturnValue(of(permittedUser({ mustChangePassword: true })));
    fixture.componentInstance.form.setValue({ email: 'ada@localhost', password: 'Temp!Password1' });
    fixture.componentInstance.submit();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/change-password');
  });
});
