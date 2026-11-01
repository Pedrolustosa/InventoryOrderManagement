export function formatPeriodKey(key: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(key);
  if (!match) {
    return key;
  }

  return `${match[3]}/${match[2]}/${match[1]}`;
}

export function formatChartValue(value: number, format: 'currency' | 'integer'): string {
  if (format === 'currency') {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 }).format(value);
}

export function textOrDash(value: string | null | undefined): string {
  const text = value?.trim();
  return text ? text : '—';
}

export function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
