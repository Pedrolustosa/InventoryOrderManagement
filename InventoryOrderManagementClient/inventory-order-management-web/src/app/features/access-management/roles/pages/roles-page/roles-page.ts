import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';
import { ToastService } from '../../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { Role } from '../../models/role.model';
import { RolesService } from '../../services/roles.service';

@Component({
  selector: 'app-roles-page',
  imports: [TranslatePipe, RouterLink, HasPermissionDirective],
  templateUrl: './roles-page.html',
})
export class RolesPage implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly toast = inject(ToastService);

  readonly roles = signal<Role[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly pendingDelete = signal<Role | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.rolesService.getRoles().subscribe({
      next: (roles) => this.roles.set(roles),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  confirmDelete(): void {
    const role = this.pendingDelete();
    if (!role) {
      return;
    }

    this.rolesService.deleteRole(role.id).subscribe({
      next: () => {
        this.pendingDelete.set(null);
        this.toast.successKey('ROLES.LIST.SUCCESS_DELETED', { roleName: role.name });
        this.load();
      },
      error: (error: Error) => {
        this.pendingDelete.set(null);
        this.toast.error(error.message);
      },
    });
  }
}
