import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { passwordPolicy } from '../../../../core/auth/password-policy';
import { firstAllowedPath } from '../../../../core/auth/landing';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-change-password-page',
  imports: [ReactiveFormsModule],
  templateUrl: './change-password-page.html',
})
export class ChangePasswordPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly forced = this.auth.mustChangePassword;

  readonly form = this.formBuilder.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, passwordPolicy]],
    confirmPassword: ['', Validators.required],
  });

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (this.form.invalid || value.newPassword !== value.confirmPassword) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.auth
      .changePassword({ currentPassword: value.currentPassword, newPassword: value.newPassword })
      .subscribe({
        next: () => {
          this.form.reset();
          const user = this.auth.getCurrentUser();
          void this.router.navigateByUrl(firstAllowedPath(user?.permissions ?? []));
        },
        error: (error: Error) => {
          this.errorMessage.set(error.message);
          this.isSubmitting.set(false);
        },
      });
  }

  signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}
