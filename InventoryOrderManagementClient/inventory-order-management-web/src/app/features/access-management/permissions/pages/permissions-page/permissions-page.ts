import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ToastService } from '../../../../../core/notifications/toast.service';
import { Role } from '../../../roles/models/role.model';
import { RolesService } from '../../../roles/services/roles.service';
import { PermissionGroup } from '../../models/permission.model';
import { PermissionsService } from '../../services/permissions.service';

@Component({
  selector: 'app-permissions-page',
  imports: [TranslatePipe, RouterLink],
  templateUrl: './permissions-page.html',
})
export class PermissionsPage implements OnInit {
  private readonly permissionsService = inject(PermissionsService);
  private readonly rolesService = inject(RolesService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  readonly catalog = signal<PermissionGroup[]>([]);
  readonly roles = signal<Role[]>([]);
  readonly selectedRole = signal<Role | null>(null);
  readonly selected = signal<string[]>([]);
  readonly confirming = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.permissionsService.getCatalog().subscribe({
      next: (catalog) => this.catalog.set(catalog),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
    this.rolesService.getRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        const roleId = this.route.snapshot.queryParamMap.get('roleId');
        const role = roles.find((item) => item.id === roleId) ?? null;
        if (role) {
          this.choose(role);
        }
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  choose(role: Role | undefined): void {
    if (!role) {
      this.selectedRole.set(null);
      this.selected.set([]);
      return;
    }

    this.selectedRole.set(role);
    this.confirming.set(false);
    this.permissionsService.getByRole(role.id).subscribe({
      next: (permissions) => this.selected.set(permissions),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  isReadOnly(): boolean {
    return this.selectedRole()?.name === 'Administrator';
  }

  toggle(permission: string): void {
    if (this.isReadOnly()) {
      return;
    }

    const current = this.selected();
    this.selected.set(current.includes(permission) ? current.filter((item) => item !== permission) : [...current, permission]);
  }

  save(): void {
    const role = this.selectedRole();
    if (!role || this.isReadOnly() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.rolesService.updatePermissions(role.id, this.selected()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.confirming.set(false);
        this.toast.successKey('PERMISSIONS.FORM.SUCCESS');
      },
      error: (error: Error) => {
        this.isSaving.set(false);
        this.confirming.set(false);
        this.toast.error(error.message);
      },
    });
  }
}
