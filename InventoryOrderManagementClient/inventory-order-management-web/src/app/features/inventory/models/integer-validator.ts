import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function integerAtLeast(minimum: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = control.value;
    if (raw === null || raw === undefined || raw === '') {
      return null;
    }

    const value = typeof raw === 'number' ? raw : Number(raw);
    if (!Number.isFinite(value) || !Number.isInteger(value)) {
      return { integer: true };
    }

    if (value < minimum) {
      return { min: { min: minimum, actual: value } };
    }

    return null;
  };
}
