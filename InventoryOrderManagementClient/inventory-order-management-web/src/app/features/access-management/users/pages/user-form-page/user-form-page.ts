import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { passwordPolicy } from '../../../../../core/auth/password-policy';
import { AuthService } from '../../../../../core/auth/services/auth.service';
import { Role } from '../../../roles/models/role.model';
import { RolesService } from '../../../roles/services/roles.service';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-user-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './user-form-page.html',
})
export class UserFormPage implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly rolesService = inject(RolesService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly userId = this.route.snapshot.paramMap.get('id');
  readonly roles = signal<Role[]>([]);
  readonly selectedRoleIds = signal<string[]>([]);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.maxLength(30)],
    password: ['', this.userId ? [] : [Validators.required, passwordPolicy]],
  });

  ngOnInit(): void {
    this.rolesService.getRoles().subscribe({
      next: (roles) => {
        const mine = this.auth.getCurrentUser()?.permissions ?? [];
        this.roles.set(roles.filter((role) => role.permissions.every((permission) => mine.includes(permission))));
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });

    if (!this.userId) {
      return;
    }

    this.usersService.getUser(this.userId).subscribe({
      next: (user) => {
        this.form.patchValue({
          displayName: user.displayName,
          email: user.email,
          phoneNumber: user.phoneNumber ?? '',
        });
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  toggleRole(roleId: string): void {
    const current = this.selectedRoleIds();
    this.selectedRoleIds.set(current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId]);
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    const phoneNumber = value.phoneNumber.trim() || null;
    const done = {
      next: () => {
        this.form.controls.password.reset('');
        void this.router.navigateByUrl('/users');
      },
      error: (error: Error) => {
        this.errorMessage.set(error.message);
        this.isSubmitting.set(false);
      },
    };

    if (this.userId) {
      this.usersService
        .updateUser(this.userId, {
          displayName: value.displayName.trim(),
          email: value.email.trim(),
          phoneNumber,
        })
        .subscribe(done);
      return;
    }

    this.usersService
      .createUser({
        displayName: value.displayName.trim(),
        email: value.email.trim(),
        phoneNumber,
        password: value.password,
        roleIds: this.selectedRoleIds(),
      })
      .subscribe(done);
  }
}
