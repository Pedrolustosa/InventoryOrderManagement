export function toCsv(rows: string[][]): string {
  return rows
    .map((row) => row.map((cell) => escapeCell(cell ?? '')).join(','))
    .join('\r\n');
}

export function downloadCsv(filename: string, rows: string[][]): void {
  const blob = new Blob([toCsv(rows)], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}

function escapeCell(value: string): string {
  if (/[",\r\n]/.test(value)) {
    return `"${value.replaceAll('"', '""')}"`;
  }

  return value;
}
