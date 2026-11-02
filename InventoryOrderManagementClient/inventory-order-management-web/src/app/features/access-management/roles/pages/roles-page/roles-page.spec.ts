import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../../core/auth/testing/permitted-auth';
import { ToastService } from '../../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../../core/notifications/testing/toast-testing';
import { RolesService } from '../../services/roles.service';
import { RolesPage } from './roles-page';

describe('RolesPage', () => {
  let fixture: ComponentFixture<RolesPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RolesPage],
      providers: [provideTranslateService(), 
        provideRouter([]),
        { provide: AuthService, useValue: permittedAuth() },
        { provide: ToastService, useValue: createToastServiceSpy() },
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
    expect(text).toContain('ROLES.LIST.SYSTEM');
    expect(text).toContain('ROLES.LIST.CUSTOM');
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows[0].textContent).not.toContain('ROLES.LIST.DELETE');
    expect(rows[1].textContent).toContain('ROLES.LIST.DELETE');
  });
});
