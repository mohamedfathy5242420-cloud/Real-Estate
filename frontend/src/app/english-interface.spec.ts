import { AccountService } from './services/account.service';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { APP_LOCALE } from './app-locale';
import { provideDemoCatalog } from './api/demo-catalog-reader';

const pages = [
  ['/catalog', 'Find your next property'],
  ['/catalog/demo-cedar-01', 'Garden apartment'],
  ['/auth/login', 'Sign in'],
  ['/auth/register', 'Create account'],
  ['/auth/recovery', 'Reset password'],
  ['/my/viewings', 'My viewings'],
  ['/employee/viewings', 'Viewing coordination and follow-up'],
  ['/admin/clients', 'Assigned customers and follow-up'],
  ['/my/profile', 'My profile and account links'],
  ['/admin/employees', 'Employee profiles'],
  ['/admin/customer-links', 'Review account link requests'],
  ['/employee/clients', 'Assigned customers'],
  ['/admin/developers', 'Catalog management'],
  ['/my/bookings', 'My bookings'],
  ['/employee/bookings', 'Bookings and external records'],
  ['/admin/bookings', 'Bookings and external records'],
  ['/admin/deals', 'Deals and commissions'],
  ['/admin/reports', 'Sales reports'],
  ['/employee/reports', 'Sales reports'],
  ['/notifications', 'Notifications'],
  ['/missing-page', 'Page not found'],
];

describe('English interface across every existing route', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: AccountService,
          useValue: {
            start: () => undefined,
            account: () => null,
            ensure: async () => ({
              accountId: 'test',
              email: 'test@brokerhub.test',
              roles: ['Customer', 'SalesEmployee', 'SalesManager', 'SystemAdministrator'],
            }),
          },
        },
        provideRouter(routes),
        ...provideDemoCatalog(),
        { provide: LOCALE_ID, useValue: APP_LOCALE },
      ],
    });
  });

  it.each(pages)(
    'localizes shell, content, accessible labels and metadata on %s',
    async (url, heading) => {
      const fixture = TestBed.createComponent(App);
      fixture.detectChanges();
      await TestBed.inject(Router).navigateByUrl(url);
      await fixture.whenStable();
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('#route-heading')?.textContent).toContain(heading);
      // Account addresses/opaque server values remain unchanged and direction-isolated.
      // Every surrounding UI string still has to be English.
      const localizable = root.cloneNode(true) as HTMLElement;
      localizable.querySelectorAll('bdi').forEach((element) => element.remove());
      expect(localizable.textContent).not.toMatch(/[\u0600-\u06ff]/);
      expect(localizable.textContent).toMatch(/[A-Za-z]/);
      expect(document.documentElement.lang).toBe('en');
      expect(document.documentElement.dir).toBe('ltr');
      expect(document.body.dir).toBe('ltr');
      expect(TestBed.inject(LOCALE_ID)).toBe('en-US');
      for (const element of root.querySelectorAll('[aria-label], [placeholder], [title]')) {
        for (const name of ['aria-label', 'placeholder', 'title']) {
          const value = element.getAttribute(name);
          if (value) {
            expect(value).toMatch(/[A-Za-z]/);
            expect(value).not.toMatch(/[\u0600-\u06ff]/);
          }
        }
      }
      expect(TestBed.inject(Title).getTitle()).toContain('BrokerHub');
      expect(TestBed.inject(Title).getTitle()).not.toMatch(/[\u0600-\u06ff]/);
      expect(TestBed.inject(Meta).getTag('name="description"')?.content).not.toMatch(
        /[\u0600-\u06ff]/,
      );
      expect(TestBed.inject(Meta).getTag('name="robots"')?.content).toBe('noindex, nofollow');
      if (url === '/catalog') {
        expect(root.querySelectorAll('app-property-card').length).toBe(6);
        expect(root.querySelector('.result-count')?.textContent).toContain('6 demo properties');
        expect(root.querySelector('.property-price bdi')?.textContent).toContain('Egyptian pounds');
      }
    },
  );
});
