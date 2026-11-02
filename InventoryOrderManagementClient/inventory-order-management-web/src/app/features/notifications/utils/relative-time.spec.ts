import { formatRelativeTime } from './relative-time';

describe('formatRelativeTime', () => {
  it('formats recent times in English', () => {
    const now = Date.parse('2026-11-02T12:10:00Z');
    const value = formatRelativeTime('2026-11-02T12:00:00Z', 'en-US', now);
    expect(value).toContain('minute');
  });

  it('formats recent times in Portuguese', () => {
    const now = Date.parse('2026-11-02T12:10:00Z');
    const value = formatRelativeTime('2026-11-02T12:00:00Z', 'pt-BR', now);
    expect(value.toLowerCase()).toContain('minuto');
  });
});
