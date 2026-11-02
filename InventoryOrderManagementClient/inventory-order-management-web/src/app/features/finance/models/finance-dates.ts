import { Pipe, PipeTransform } from '@angular/core';

export function formatDateOnly(value: string | null | undefined): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value ?? '');
  if (!match) {
    return '—';
  }

  return `${match[3]}/${match[2]}/${match[1]}`;
}

export function localDateTimeInput(value = new Date()): string {
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`;
}

export function localDateTimeToUtc(value: string): string | null {
  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return null;
  }

  return parsed.toISOString();
}

@Pipe({
  name: 'dateOnly',
})
export class DateOnlyPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDateOnly(value);
  }
}
