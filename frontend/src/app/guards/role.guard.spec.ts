import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { roleGuard } from './role.guard';
import { AccountService } from '../services/account.service';
import { vi } from 'vitest';
@Component({ template: '<p>restricted-staff-screen</p>' })
class RestrictedScreen {}
@Component({ template: '<p>safe-screen</p>' })
class SafeScreen {}
describe('FAC-02/11 role routing', () => {
  function setup(roles: string[] | null, unavailable = false) {
    const ensure = unavailable
      ? vi.fn().mockRejectedValue(new Error('unavailable'))
      : vi
          .fn()
          .mockResolvedValue(
            roles ? { accountId: 'own', email: 'own@brokerhub.test', roles } : null,
          );
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'admin/staff-accounts',
            component: RestrictedScreen,
            canActivate: [roleGuard],
            data: { roles: ['SystemAdministrator'] },
          },
          { path: 'auth/login', component: SafeScreen },
          { path: 'access', component: SafeScreen },
        ]),
        {
          provide: AccountService,
          useValue: { ensure, status: () => (unavailable ? 'unavailable' : 'guest') },
        },
      ],
    });
    return ensure;
  }
  it('redirects a guest using only the safe explicit return route', async () => {
    const ensure = setup(null);
    const harness = await RouterTestingHarness.create('/admin/staff-accounts');
    expect(TestBed.inject(Router).url).toBe('/auth/login?returnUrl=%2Fadmin%2Fstaff-accounts');
    expect(harness.routeNativeElement?.textContent).not.toContain('restricted-staff-screen');
    expect(ensure).toHaveBeenCalledWith(true);
  });
  for (const role of ['Customer', 'SalesEmployee', 'SalesManager'])
    it(`denies ${role} without implicit Administrator inheritance`, async () => {
      setup([role]);
      const harness = await RouterTestingHarness.create('/admin/staff-accounts');
      expect(TestBed.inject(Router).url).toBe('/access?reason=forbidden');
      expect(harness.routeNativeElement?.textContent).not.toContain('restricted-staff-screen');
    });
  it('allows a verified Administrator', async () => {
    setup(['SystemAdministrator']);
    const harness = await RouterTestingHarness.create('/admin/staff-accounts');
    expect(harness.routeNativeElement?.textContent).toContain('restricted-staff-screen');
  });
  it('shows unavailable rather than pretending the user is an anonymous guest', async () => {
    setup(null, true);
    await RouterTestingHarness.create('/admin/staff-accounts');
    expect(TestBed.inject(Router).url).toContain('/access?reason=unavailable');
  });
});
