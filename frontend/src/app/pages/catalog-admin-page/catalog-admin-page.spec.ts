import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CatalogAdminPage } from './catalog-admin-page';
import { CatalogAdminService } from '../../services/catalog-admin.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';

describe('FAC-01 commission rule input switching', () => {
  it('clears the identity boundary on an explicit catalog Administrator denial', async () => {
    const service = {
      references: async () => {
        throw new ApiFailure('forbidden');
      },
      list: async () => [],
    };
    TestBed.configureTestingModule({
      imports: [CatalogAdminPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of(convertToParamMap({ section: 'developers' })),
            snapshot: { queryParamMap: convertToParamMap({}) },
          },
        },
      ],
    });
    TestBed.overrideComponent(CatalogAdminPage, {
      set: { providers: [{ provide: CatalogAdminService, useValue: service }] },
    });
    const context = TestBed.inject(SessionContext);
    context.accept(context.revision(), {
      accountId: 'own',
      email: 'private-admin@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
    const fixture = TestBed.createComponent(CatalogAdminPage);
    await fixture.whenStable();
    expect(context.status()).toBe('forbidden');
    expect(context.account()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('#field-code')).toBeNull();
  });
  it('binds the visible fixed amount to its own control after changing type', async () => {
    const owner = '11111111-1111-1111-1111-111111111111';
    const save = vi.fn().mockResolvedValue(undefined);
    const service = {
      references: async () => ({
        developers: [
          { kind: 'developers', id: owner, code: 'TEST', name: 'Fictional', isActive: true },
        ],
        projects: [],
      }),
      list: async () => [],
      save,
    };
    TestBed.configureTestingModule({
      imports: [CatalogAdminPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of(convertToParamMap({ section: 'agreements' })),
            snapshot: { queryParamMap: convertToParamMap({}) },
          },
        },
      ],
    });
    TestBed.overrideComponent(CatalogAdminPage, {
      set: { providers: [{ provide: CatalogAdminService, useValue: service }] },
    });
    const context = TestBed.inject(SessionContext);
    context.accept(context.revision(), {
      accountId: owner,
      email: 'admin@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
    const fixture = TestBed.createComponent(CatalogAdminPage);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    function set(id: string, value: string, event = 'input') {
      const el = root.querySelector('#' + id) as HTMLInputElement;
      el.value = value;
      el.dispatchEvent(new Event(event));
      fixture.detectChanges();
    }
    set('field-developerId', owner, 'change');
    set('field-agreementNumber', 'RULE-TEST');
    set('field-startsOn', '2026-01-01');
    set('field-endsOn', '2027-12-31');
    set('rule-value-0', '2.5001');
    set('rule-type-0', 'FixedAmount', 'change');
    set('rule-value-0', '1234.56');
    set('rule-currency-0', 'EGP');
    root
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
    expect(save).toHaveBeenCalledTimes(1);
    expect(save.mock.calls[0][4][0]).toMatchObject({
      ruleType: 'FixedAmount',
      fixedAmount: '1234.56',
      percentage: '2.5001',
    });
  });
});
