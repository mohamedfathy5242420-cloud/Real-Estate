import { Account } from './account';

export const STAFF_ROLES = ['SalesEmployee', 'SalesManager', 'SystemAdministrator'] as const;
export type StaffRole = (typeof STAFF_ROLES)[number];
export const ROLE_LABELS: Record<string, string> = {
  Customer: 'Customer',
  SalesEmployee: 'Sales employee',
  SalesManager: 'Sales manager',
  SystemAdministrator: 'System administration',
};
export interface WorkDestination {
  path: string;
  title: string;
  roles: readonly string[];
  ready: boolean;
}
export const WORK_DESTINATIONS: readonly WorkDestination[] = [
  { path: '/my/profile', title: 'My profile and account links', roles: ['Customer'], ready: true },
  {
    path: '/admin/employees',
    title: 'Employee profiles',
    roles: ['SystemAdministrator'],
    ready: true,
  },
  {
    path: '/admin/staff-accounts',
    title: 'Create staff account',
    roles: ['SystemAdministrator'],
    ready: true,
  },
  {
    path: '/admin/developers',
    title: 'Manage listings',
    roles: ['SystemAdministrator'],
    ready: true,
  },
  { path: '/admin/clients', title: 'Assign customers', roles: ['SalesManager'], ready: true },
  {
    path: '/admin/customer-links',
    title: 'Review account link requests',
    roles: ['SalesManager'],
    ready: true,
  },
  { path: '/employee/clients', title: 'Assigned customers', roles: ['SalesEmployee'], ready: true },
  {
    path: '/employee/viewings',
    title: 'Manage viewings',
    roles: ['SalesEmployee', 'SalesManager'],
    ready: true,
  },
  {
    path: '/employee/bookings',
    title: 'Manage booking requests',
    roles: ['SalesEmployee'],
    ready: true,
  },
  { path: '/admin/bookings', title: 'Booking management', roles: ['SalesManager'], ready: true },
  { path: '/admin/deals', title: 'Deals and commissions', roles: ['SalesManager'], ready: true },
  { path: '/admin/reports', title: 'Sales reports', roles: ['SalesManager'], ready: true },
  { path: '/employee/reports', title: 'Activity reports', roles: ['SalesEmployee'], ready: true },
  { path: '/my/viewings', title: 'My viewings', roles: ['Customer'], ready: true },
  { path: '/my/bookings', title: 'My booking requests', roles: ['Customer'], ready: true },
  {
    path: '/notifications',
    title: 'Notifications',
    roles: ['Customer', 'SalesEmployee', 'SalesManager'],
    ready: true,
  },
];
export function canVisit(account: Account | null, roles: readonly string[]): boolean {
  return !!account && roles.some((role) => account.roles.includes(role));
}
export function safeReturnUrl(candidate: string | null): string {
  return candidate &&
    (/^\/catalog(?:\/[0-9a-f-]{36})?(?:\?[^#]*)?$/i.test(candidate) ||
      /^\/my\/(?:viewings|bookings)\?unitId=[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(
        candidate,
      ) ||
      candidate === '/workspace' ||
      WORK_DESTINATIONS.some((item) => item.path === candidate))
    ? candidate
    : '/auth/session';
}
