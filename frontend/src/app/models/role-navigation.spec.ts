import { WORK_DESTINATIONS, canVisit, safeReturnUrl } from './role-navigation';
describe('D034/D049 role boundaries and safe destinations', () => {
  for (const role of ['Customer', 'SalesEmployee', 'SalesManager', 'SystemAdministrator']) {
    it(`${role} receives only its actual destinations, with unfinished work identified`, () => {
      const account = { accountId: 'own', email: 'own@brokerhub.test', roles: [role] };
      const paths = WORK_DESTINATIONS.filter((item) => canVisit(account, item.roles)).map(
        (item) => item.path,
      );
      expect(paths.includes('/admin/staff-accounts')).toBe(role === 'SystemAdministrator');
      expect(paths.includes('/admin/deals')).toBe(role === 'SalesManager');
      expect(paths.includes('/my/bookings')).toBe(role === 'Customer');
      expect(paths.includes('/employee/clients')).toBe(role === 'SalesEmployee');
      expect(paths.includes('/my/profile')).toBe(role === 'Customer');
      expect(paths.includes('/notifications')).toBe(role !== 'SystemAdministrator');
      expect(paths.includes('/admin/employees')).toBe(role === 'SystemAdministrator');
      expect(WORK_DESTINATIONS.find((item) => item.path === '/my/profile')?.ready).toBe(true);
      expect(WORK_DESTINATIONS.find((item) => item.path === '/admin/employees')?.ready).toBe(true);
    });
  }
  it('does not treat unknown roles or a guest as authorized', () => {
    expect(
      canVisit({ accountId: 'own', email: 'own@brokerhub.test', roles: ['admin'] }, [
        'SystemAdministrator',
      ]),
    ).toBe(false);
    expect(canVisit(null, ['Customer'])).toBe(false);
  });
  it('accepts only explicit internal destinations and never accepts auth/callback/external routes', () => {
    for (const url of [
      'https://evil.invalid',
      '//evil.invalid',
      '/auth/reset-password?token=secret',
      '/admin/staff-accounts?role=admin',
      '/workspace/../admin',
    ])
      expect(safeReturnUrl(url)).toBe('/auth/session');
    expect(safeReturnUrl('/admin/staff-accounts')).toBe('/admin/staff-accounts');
    const viewing = '/my/viewings?unitId=11111111-1111-1111-1111-111111111111';
    expect(safeReturnUrl(viewing)).toBe(viewing);
    expect(safeReturnUrl(viewing + '&role=SystemAdministrator')).toBe('/auth/session');
  });
});
