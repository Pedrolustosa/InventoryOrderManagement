import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { Role } from '../../models/role.model';
import { RolesService } from '../../services/roles.service';

@Component({
  selector: 'app-roles-page',
  imports: [RouterLink, HasPermissionDirective],
  templateUrl: './roles-page.html',
})
export class RolesPage implements OnInit {
  private readonly rolesService = inject(RolesService);

  readonly roles = signal<Role[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
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
        this.successMessage.set(`${role.name} was deleted.`);
        this.load();
      },
      error: (error: Error) => {
        this.pendingDelete.set(null);
        this.errorMessage.set(error.message);
      },
    });
  }
}
