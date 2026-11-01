import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RolesService } from '../../services/roles.service';

@Component({
  selector: 'app-role-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './role-form-page.html',
})
export class RoleFormPage implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  readonly roleId = this.route.snapshot.paramMap.get('id');
  readonly isSystemRole = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    description: ['', Validators.maxLength(300)],
  });

  ngOnInit(): void {
    if (!this.roleId) {
      return;
    }

    this.rolesService.getRole(this.roleId).subscribe({
      next: (role) => {
        this.isSystemRole.set(role.isSystemRole);
        this.form.patchValue({ name: role.name, description: role.description ?? '' });
        if (role.isSystemRole) {
          this.form.controls.name.disable();
        }
      },
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isSubmitting()) {
      return;
    }

    const value = this.form.getRawValue();
    const body = { name: value.name.trim(), description: value.description.trim() || null };
    this.isSubmitting.set(true);
    const done = {
      next: () => void this.router.navigateByUrl('/roles'),
      error: (error: Error) => {
        this.errorMessage.set(error.message);
        this.isSubmitting.set(false);
      },
    };

    if (this.roleId) {
      this.rolesService.updateRole(this.roleId, body).subscribe(done);
      return;
    }

    this.rolesService.createRole(body).subscribe(done);
  }
}
