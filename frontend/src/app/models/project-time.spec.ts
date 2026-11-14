import { formatProjectTime } from './project-time';
import { WINDOWS_TIME_ZONES } from './windows-time-zones';
describe('D030/AC-06 actual project zone display', () => {
  const time = '2026-11-07T09:00:00.1234567Z';
  it('accepts native IANA and the equivalent current Backend Windows ID case-insensitively', () => {
    expect(WINDOWS_TIME_ZONES['egypt standard time']).toBe('Africa/Cairo');
    expect(formatProjectTime(time, 'Egypt Standard Time')).toBe(
      formatProjectTime(time, 'Africa/Cairo'),
    );
    expect(formatProjectTime(time, 'egypt standard time')).toBe(
      formatProjectTime(time, 'Africa/Cairo'),
    );
    expect(formatProjectTime(time, 'Egypt Standard Time')).not.toBe(formatProjectTime(time, 'UTC'));
  });
  it('has browser-supported canonical names for all138 generated Windows aliases', () => {
    expect(Object.keys(WINDOWS_TIME_ZONES)).toHaveLength(138);
    for (const [name, iana] of Object.entries(WINDOWS_TIME_ZONES)) {
      expect(() => new Intl.DateTimeFormat('en-US', { timeZone: iana })).not.toThrow();
      expect(formatProjectTime(time, name)).not.toContain('(UTC)');
    }
  });
  it('labels UTC fallback honestly when the browser has no zone instead of showing a wrong local time', () => {
    expect(formatProjectTime(time, 'unknown-zone')).toBe('2026-11-07T09:00:00.123Z (UTC)');
  });
  it('handles absent or invalid timestamps without inventing an appointment', () => {
    expect(formatProjectTime(null, 'UTC')).toBe('Not recorded');
    expect(formatProjectTime('invalid', 'Africa/Cairo')).toBe('Could not display time');
  });
});
