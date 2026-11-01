import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { Role } from '../../../roles/models/role.model';
import { RolesService } from '../../../roles/services/roles.service';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-assign-roles-page',
  imports: [RouterLink],
  templateUrl: './assign-roles-page.html',
})
export class AssignRolesPage implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly roles = signal<Role[]>([]);
  readonly selected = signal<string[]>([]);
  readonly userName = signal('');
  readonly confirming = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private readonly userId = this.route.snapshot.paramMap.get('id') ?? '';

  ngOnInit(): void {
    if (this.userId === this.auth.getCurrentUser()?.id) {
      void this.router.navigateByUrl('/access-denied');
      return;
    }

    this.usersService.getUser(this.userId).subscribe({
      next: (user) => {
        this.userName.set(user.displayName);
        this.selected.set(user.roles.map((role) => role.id));
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });

    this.rolesService.getRoles().subscribe({
      next: (roles) => {
        const mine = this.auth.getCurrentUser()?.permissions ?? [];
        this.roles.set(roles.filter((role) => role.permissions.every((permission) => mine.includes(permission))));
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  toggle(roleId: string): void {
    const current = this.selected();
    this.selected.set(current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId]);
  }

  save(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.usersService.assignRoles(this.userId, this.selected()).subscribe({
      next: () => void this.router.navigate(['/users', this.userId]),
      error: (error: Error) => {
        this.errorMessage.set(error.message);
        this.isSubmitting.set(false);
        this.confirming.set(false);
      },
    });
  }
}
