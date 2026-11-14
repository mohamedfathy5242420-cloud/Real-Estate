import { WINDOWS_TIME_ZONES } from './windows-time-zones';
/** Display only. Original UTC strings/versions remain unchanged in models and commands. */
export function formatProjectTime(value: string | null, zone: string): string {
  if (!value) return 'Not recorded';
  const date = new Date(value);
  if (!Number.isFinite(date.getTime())) return 'Could not display time';
  const name = zone.trim();
  const key = name.toLowerCase();
  const browserZone = Object.hasOwn(WINDOWS_TIME_ZONES, key) ? WINDOWS_TIME_ZONES[key] : name;
  try {
    return new Intl.DateTimeFormat('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: browserZone,
    }).format(date);
  } catch {
    return date.toISOString() + ' (UTC)';
  }
}
