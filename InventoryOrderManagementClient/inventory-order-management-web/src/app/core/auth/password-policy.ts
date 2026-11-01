import { AbstractControl, ValidationErrors } from '@angular/forms';

export function passwordPolicy(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  if (
    value.length >= 12 &&
    /[A-Z]/.test(value) &&
    /[a-z]/.test(value) &&
    /\d/.test(value) &&
    /[^A-Za-z0-9]/.test(value)
  ) {
    return null;
  }

  return { passwordPolicy: true };
}
