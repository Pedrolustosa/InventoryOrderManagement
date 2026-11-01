import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../../core/auth/testing/permitted-auth';
import { RolesService } from '../../services/roles.service';
import { RolesPage } from './roles-page';

describe('RolesPage', () => {
  let fixture: ComponentFixture<RolesPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RolesPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: permittedAuth() },
        {
          provide: RolesService,
          useValue: {
            getRoles: () =>
              of([
                { id: 'admin', name: 'Administrator', description: null, isSystemRole: true, permissions: [] },
                { id: 'custom', name: 'Floor', description: 'Custom', isSystemRole: false, permissions: [] },
              ]),
          },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(RolesPage);
    fixture.detectChanges();
  });

  it('marks system and custom roles and offers delete only for the custom role', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('System');
    expect(text).toContain('Custom');
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows[0].textContent).not.toContain('Delete');
    expect(rows[1].textContent).toContain('Delete');
  });
});
