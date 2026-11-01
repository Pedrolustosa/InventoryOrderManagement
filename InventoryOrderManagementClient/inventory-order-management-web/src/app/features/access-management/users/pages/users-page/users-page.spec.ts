import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../../core/auth/testing/permitted-auth';
import { RolesService } from '../../../roles/services/roles.service';
import { UsersService } from '../../services/users.service';
import { UsersPage } from './users-page';

describe('UsersPage', () => {
  let fixture: ComponentFixture<UsersPage>;
  let usersService: { getUsers: ReturnType<typeof vi.fn>; deactivateUser: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    usersService = {
      getUsers: vi.fn().mockReturnValue(
        of({
          items: [
            {
              id: 'user-1',
              displayName: 'Ada',
              email: 'ada@localhost',
              phoneNumber: null,
              isActive: true,
              mustChangePassword: true,
              roles: [{ id: 'role-1', name: 'Viewer' }],
              createdAtUtc: '2026-11-01T12:00:00Z',
              updatedAtUtc: null,
              lastLoginAtUtc: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalItems: 1,
          totalPages: 1,
        }),
      ),
      deactivateUser: vi.fn().mockReturnValue(throwError(() => new Error('The last active administrator cannot be deactivated.'))),
    };

    await TestBed.configureTestingModule({
      imports: [UsersPage],
      providers: [
        provideRouter([]),
        { provide: UsersService, useValue: usersService },
        { provide: RolesService, useValue: { getRoles: () => of([]) } },
        { provide: AuthService, useValue: permittedAuth(permittedUser({ id: 'other-user' })) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsersPage);
    fixture.detectChanges();
  });

  it('lists users in the order returned by the API', () => {
    expect(fixture.nativeElement.textContent).toContain('Ada');
    expect(fixture.nativeElement.textContent).toContain('Viewer');
  });

  it('shows the conflict detail when deactivation is rejected', () => {
    fixture.componentInstance.pendingDeactivate.set(fixture.componentInstance.users()[0]);
    fixture.componentInstance.confirmDeactivate();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('The last active administrator cannot be deactivated.');
  });
});
