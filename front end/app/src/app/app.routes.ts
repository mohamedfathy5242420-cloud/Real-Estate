import { Routes } from '@angular/router';
import { FoundationPage } from './pages/foundation-page/foundation-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },
  {
    path: 'catalog',
    component: FoundationPage,
    data: {
      eyebrow: 'Public route · SSR foundation',
      title: 'Property catalog',
      description: 'Catalog data, filters and paging are not connected in this foundation step.',
    },
  },
  {
    path: 'catalog/:unitId',
    component: FoundationPage,
    data: {
      eyebrow: 'Public route · SSR foundation',
      title: 'Unit details',
      description:
        'Unit data and request actions will be implemented against an agreed public API contract.',
    },
  },
  {
    path: 'auth/login',
    component: FoundationPage,
    data: {
      eyebrow: 'Nonindexed route · CSR foundation',
      title: 'Customer sign in',
      description:
        'Email and password are selected, but session transport, verification and recovery remain gated by Q01.',
    },
  },
  {
    path: 'auth/register',
    component: FoundationPage,
    data: {
      eyebrow: 'Nonindexed route · CSR foundation',
      title: 'Create account',
      description: 'Registration is not implemented.',
    },
  },
  {
    path: 'auth/recovery',
    component: FoundationPage,
    data: {
      eyebrow: 'Nonindexed route · CSR foundation',
      title: 'Recover access',
      description: 'Recovery is not implemented.',
    },
  },
  ...[
    ['my/viewings', 'My viewings'],
    ['employee/viewings', 'Viewing work queue'],
    ['admin/clients', 'Client assignment'],
    ['employee/clients', 'Assigned clients'],
    ['admin/developers', 'Catalog administration'],
    ['my/bookings', 'My booking requests'],
    ['employee/bookings', 'Booking work queue'],
    ['admin/bookings', 'Booking oversight'],
    ['admin/deals', 'Deals and commissions'],
    ['notifications', 'Notifications'],
  ].map(([path, title]) => ({
    path,
    component: FoundationPage,
    data: {
      eyebrow: 'Protected route placeholder · CSR foundation',
      title,
      description:
        'This route exists for foundation verification only. Authorization and live data are not implemented.',
    },
  })),
  {
    path: '**',
    component: FoundationPage,
    data: {
      eyebrow: 'Navigation',
      title: 'Page not found',
      description: 'The requested foundation route does not exist.',
    },
  },
];
