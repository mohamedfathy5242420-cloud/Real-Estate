import { Route, Routes } from '@angular/router';
import { RoutePresentationData } from './models/route-presentation';
import { WORK_DESTINATIONS } from './models/role-navigation';
import { roleGuard } from './guards/role.guard';

const loadFoundationPage = () =>
  import('./pages/foundation-page/foundation-page').then((module) => module.FoundationPage);

function foundationRoute(path: string, data: RoutePresentationData): Route {
  return {
    path,
    loadComponent: loadFoundationPage,
    data,
  };
}

const accountTitles = {
  login: 'Sign in',
  register: 'Create account',
  recovery: 'Reset password',
  'confirm-email': 'Confirm email',
  'reset-password': 'Set a new password',
  session: 'My account',
};

export const routes: Routes = [
  {
    path: 'notifications',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/notifications-page/notifications-page').then((m) => m.NotificationsPage),
    data: {
      title: 'Notifications',
      roles: ['Customer', 'SalesEmployee', 'SalesManager'],
      indexable: false,
    },
  },
  {
    path: 'admin/deals',
    canActivate: [roleGuard],
    loadComponent: () => import('./pages/sales-page/sales-page').then((m) => m.SalesPage),
    data: { title: 'Deals and commissions', roles: ['SalesManager'], indexable: false },
  },
  ...(['admin/reports', 'employee/reports'] as const).map((path) => ({
    path,
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/sales-reports-page/sales-reports-page').then((m) => m.SalesReportsPage),
    data: {
      title: 'Sales reports',
      manager: path === 'admin/reports',
      roles: path === 'admin/reports' ? ['SalesManager'] : ['SalesEmployee'],
      indexable: false,
    },
  })),
  ...(['my/bookings', 'employee/bookings', 'admin/bookings'] as const).map((path) => ({
    path,
    canActivate: [roleGuard],
    loadComponent: () => import('./pages/bookings-page/bookings-page').then((m) => m.BookingsPage),
    data: {
      title: path === 'my/bookings' ? 'My bookings' : 'Bookings and external records',
      staff: path !== 'my/bookings',
      roles:
        path === 'my/bookings'
          ? ['Customer']
          : path === 'admin/bookings'
            ? ['SalesManager']
            : ['SalesEmployee'],
      indexable: false,
    },
  })),
  ...(['my/viewings', 'employee/viewings'] as const).map((path) => ({
    path,
    canActivate: [roleGuard],
    loadComponent: () => import('./pages/viewings-page/viewings-page').then((m) => m.ViewingsPage),
    data: {
      title: path === 'my/viewings' ? 'My viewings' : 'Viewing coordination and follow-up',
      staff: path === 'employee/viewings',
      roles: path === 'my/viewings' ? ['Customer'] : ['SalesEmployee', 'SalesManager'],
      indexable: false,
    },
  })),
  ...(['admin/clients', 'employee/clients'] as const).map((path) => ({
    path,
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/crm-page/crm-page').then((module) => module.CrmPageComponent),
    data: {
      title: 'Customers and follow-up',
      roles: path === 'admin/clients' ? ['SalesManager'] : ['SalesEmployee'],
      indexable: false,
    },
  })),
  {
    path: 'admin/customer-links',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/customer-links-page/customer-links-page').then(
        (module) => module.CustomerLinksPage,
      ),
    data: { title: 'Review account link requests', roles: ['SalesManager'], indexable: false },
  },
  {
    path: 'my/profile',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/customer-profile-page/customer-profile-page').then(
        (module) => module.CustomerProfilePage,
      ),
    data: { title: 'My profile and account links', roles: ['Customer'], indexable: false },
  },
  {
    path: 'admin/employees',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/employees-page/employees-page').then((module) => module.EmployeesPage),
    data: { title: 'Employee profiles', roles: ['SystemAdministrator'], indexable: false },
  },
  {
    path: 'admin/developers',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/catalog-admin-page/catalog-admin-page').then(
        (module) => module.CatalogAdminPage,
      ),
    data: {
      title: 'Catalog management',
      description: 'Developers, projects, units and marketing agreements',
      roles: ['SystemAdministrator'],
      indexable: false,
    },
  },
  {
    path: 'workspace',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/workspace-page/workspace-page').then((module) => module.WorkspacePage),
    data: { title: 'Workspace', description: 'Your current account tasks', indexable: false },
  },
  {
    path: 'access',
    loadComponent: () =>
      import('./pages/access-page/access-page').then((module) => module.AccessPage),
    data: { title: 'Access', description: 'Page access status', indexable: false },
  },
  {
    path: 'admin/staff-accounts',
    canActivate: [roleGuard],
    loadComponent: () =>
      import('./pages/staff-accounts-page/staff-accounts-page').then(
        (module) => module.StaffAccountsPage,
      ),
    data: {
      title: 'Create staff account',
      description: 'Staff sign-in accounts',
      roles: ['SystemAdministrator'],
      indexable: false,
    },
  },
  ...(['login', 'register', 'recovery', 'confirm-email', 'reset-password', 'session'] as const).map(
    (mode) => ({
      path: `auth/${mode}`,
      loadComponent: () =>
        import('./pages/account-page/account-page').then((module) => module.AccountPage),
      data: {
        mode,
        eyebrow: 'Customer account',
        title: accountTitles[mode],
        description: 'Manage your account securely.',
        indexable: false,
      },
    }),
  ),
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },
  {
    path: 'catalog',
    loadComponent: () =>
      import('./pages/catalog-page/catalog-page').then((module) => module.CatalogPage),
    data: {
      eyebrow: 'Discover properties',
      title: 'Property catalog',
      description:
        'Browse published properties on BrokerHub. Availability is confirmed with the developer.',
      indexable: true,
    } satisfies RoutePresentationData,
  },
  {
    path: 'catalog/:unitId',
    loadComponent: () =>
      import('./pages/unit-detail-page/unit-detail-page').then((module) => module.UnitDetailPage),
    data: {
      eyebrow: 'Property details',
      title: 'Unit details',
      description:
        'Explore this unit. A published listing does not confirm availability or a booking.',
      indexable: false,
    } satisfies RoutePresentationData,
  },
  ...WORK_DESTINATIONS.filter((item) => !item.ready).map(({ path, title, roles }) => ({
    ...foundationRoute(path.slice(1), {
      eyebrow: 'Coming soon',
      title,
      description:
        'This page is being prepared. Its data, actions and permissions are not connected to the server yet.',
      indexable: false,
    }),
    canActivate: [roleGuard],
    data: {
      eyebrow: 'Coming soon',
      title,
      description: 'This page is being prepared. Actions are not implemented yet.',
      roles,
      indexable: false,
    },
  })),
  foundationRoute('**', {
    eyebrow: 'Navigation',
    title: 'Page not found',
    description: 'We could not find this page. Return to the property catalog.',
    indexable: false,
  }),
];
