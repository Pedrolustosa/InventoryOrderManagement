import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ToastService } from '../../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../../core/notifications/testing/toast-testing';
import { RolesService } from '../../../roles/services/roles.service';
import { PermissionsService } from '../../services/permissions.service';
import { PermissionsPage } from './permissions-page';

describe('PermissionsPage', () => {
  let fixture: ComponentFixture<PermissionsPage>;
  let rolesService: { getRoles: ReturnType<typeof vi.fn>; updatePermissions: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    rolesService = {
      getRoles: vi.fn().mockReturnValue(
        of([
          { id: 'admin', name: 'Administrator', description: null, isSystemRole: true, permissions: ['Users.View'] },
          { id: 'custom', name: 'Custom', description: null, isSystemRole: false, permissions: [] },
        ]),
      ),
      updatePermissions: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [PermissionsPage],
      providers: [provideTranslateService(), 
        provideRouter([]),
        { provide: PermissionsService, useValue: { getCatalog: () => of([{ module: 'Users', permissions: ['Users.View'] }]), getByRole: () => of(['Users.View']) } },
        { provide: RolesService, useValue: rolesService },
        { provide: ToastService, useValue: createToastServiceSpy() },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PermissionsPage);
    fixture.detectChanges();
  });

  it('keeps the Administrator matrix read-only', () => {
    fixture.componentInstance.choose(fixture.componentInstance.roles()[0]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('PERMISSIONS.FORM.ADMIN_FIXED');
    expect(fixture.nativeElement.querySelector('input[type="checkbox"]').disabled).toBe(true);
  });

  it('saves the complete selected list for a custom role', () => {
    fixture.componentInstance.choose(fixture.componentInstance.roles()[1]);
    fixture.componentInstance.save();
    expect(rolesService.updatePermissions).toHaveBeenCalledWith('custom', ['Users.View']);
  });
});
