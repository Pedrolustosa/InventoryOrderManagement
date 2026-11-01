import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { Role } from '../../../roles/models/role.model';
import { RolesService } from '../../../roles/services/roles.service';
import { UserAccount } from '../../models/user.model';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-users-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, HasPermissionDirective],
  templateUrl: './users-page.html',
})
export class UsersPage implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  readonly users = signal<UserAccount[]>([]);
  readonly roles = signal<Role[]>([]);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly totalPages = signal(1);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly pendingDeactivate = signal<UserAccount | null>(null);

  readonly filters = this.formBuilder.nonNullable.group({
    name: '',
    email: '',
    isActive: '',
    roleId: '',
  });

  ngOnInit(): void {
    this.rolesService.getRoles().subscribe({
      next: (roles) => this.roles.set(roles),
      error: () => this.roles.set([]),
    });
    this.load();
  }

  isSelf(user: UserAccount): boolean {
    return user.id === this.auth.getCurrentUser()?.id;
  }

  load(page = this.page()): void {
    const value = this.filters.getRawValue();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.usersService
      .getUsers({
        name: value.name.trim() || undefined,
        email: value.email.trim() || undefined,
        isActive: value.isActive === '' ? undefined : value.isActive === 'true',
        roleId: value.roleId || undefined,
        page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.page.set(result.page);
          this.totalPages.set(Math.max(result.totalPages, 1));
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.errorMessage.set(error.message);
          this.isLoading.set(false);
        },
      });
  }

  confirmDeactivate(): void {
    const user = this.pendingDeactivate();
    if (!user) {
      return;
    }

    this.usersService.deactivateUser(user.id).subscribe({
      next: () => {
        this.pendingDeactivate.set(null);
        this.successMessage.set(`${user.displayName} was deactivated.`);
        this.load();
      },
      error: (error: Error) => {
        this.pendingDeactivate.set(null);
        this.errorMessage.set(error.message);
      },
    });
  }
}
