import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ApiTransport } from './api-transport';
import { SalesApi, saleProjection, saleQuery } from './sales-api';
import {
  COMMISSION_SNAPSHOT_FIELDS,
  CURRENCY_TOTAL_FIELDS,
  ACTIVITY_REPORT_FIELDS,
  SaleFieldKind,
  SaleOperation,
} from '../models/sales';
const id = '11111111-1111-1111-1111-111111111111',
  fact = '22222222-2222-2222-2222-222222222222';
function record(fields: Readonly<Record<string, SaleFieldKind>>) {
  return Object.fromEntries(
    Object.entries(fields).map(([key, kind]) => [
      key,
      kind.endsWith('?')
        ? null
        : kind === 'id'
          ? id
          : kind === 'version'
            ? '9223372036854775807'
            : kind === 'number'
              ? 30
              : kind === 'count'
                ? '9007199254740993'
                : kind === 'money'
                  ? '9999999999999999.99'
                  : kind === 'aggregate'
                    ? '19999999999999999.98'
                    : key === 'currencyCode'
                      ? 'EGP'
                      : key === 'percentage'
                        ? '2.1250'
                        : 'saved-original',
    ]),
  );
}
describe('AC-13 exact saved sales projections and scoped receipts', () => {
  it('preserves snapshot34/exact money/rate/version and drops undeclared data', () => {
    const source = {
      ...record(COMMISSION_SNAPSHOT_FIELDS),
      percentage: '2.1250',
      fixedAmount: null,
      unexpected: 'secret',
    };
    const value = saleProjection<Record<string, unknown>>(source, COMMISSION_SNAPSHOT_FIELDS);
    expect(Object.keys(value)).toHaveLength(34);
    expect(value['amount']).toBe('9999999999999999.99');
    expect(value['percentage']).toBe('2.1250');
    expect(value['version']).toBe('9223372036854775807');
    expect(value['unexpected']).toBeUndefined();
    expect(() =>
      saleProjection({ ...source, amount: 9999999999999999 }, COMMISSION_SNAPSHOT_FIELDS),
    ).toThrow();
    expect(() =>
      saleProjection({ ...source, version: '9223372036854775808' }, COMMISSION_SNAPSHOT_FIELDS),
    ).toThrow();
  });
  it('retains currency totals beyond per-fact width and quoted Int64 activity counts', () => {
    const total = saleProjection<Record<string, string>>(
      record(CURRENCY_TOTAL_FIELDS),
      CURRENCY_TOTAL_FIELDS,
    );
    expect(total['entitledAmount']).toBe('19999999999999999.98');
    expect(total['dealCount']).toBe('9007199254740993');
    const source = record(ACTIVITY_REPORT_FIELDS);
    expect(
      saleProjection<Record<string, string>>(source, ACTIVITY_REPORT_FIELDS)['customerCount'],
    ).toBe('9007199254740993');
    expect(() =>
      saleProjection({ ...source, customerCount: 9007199254740992 }, ACTIVITY_REPORT_FIELDS),
    ).toThrow();
  });
  it('omits empty/unapproved filters, normalizes currency and keeps local dates separate from UTC dates', () => {
    const q = saleQuery(
      {
        customerId: '',
        currencyCode: 'egp',
        saleLocalFrom: '2026-11-08',
        fromUtcDate: '2026-11-01',
        actorAccountId: id,
      },
      ['customerId', 'currencyCode', 'saleLocalFrom'],
      2,
    );
    expect(q).toBe('?page=2&pageSize=20&currencyCode=EGP&saleLocalFrom=2026-11-08');
    expect(() => saleQuery({ customerId: '../accounts' }, ['customerId'])).toThrow();
    expect(() => saleQuery({}, [], 0)).toThrow();
  });
  it('routes collection receipt to its original Deal and marks malformed successful fact outcome unknown', async () => {
    const transport = {
      post: vi.fn().mockResolvedValue({ id: fact, version: '1', isReplay: true }),
    };
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: transport }] });
    const api = TestBed.inject(SalesApi);
    const op: SaleOperation = {
      action: 'collect',
      customerId: id,
      dealId: id,
      key: 'original-key',
      revision: 0,
      body: { version: '9223372036854775807', amount: '0.01', currencyCode: 'EGP' },
    };
    expect(await api.execute(op)).toEqual({ id: fact, version: '1', isReplay: true });
    expect(transport.post).toHaveBeenCalledExactlyOnceWith(
      `/api/crm/customers/${id}/deals/${id}/collections`,
      op.body,
      true,
      op.key,
    );
    transport.post.mockResolvedValue({ id: fact, version: 1, isReplay: false });
    await expect(api.execute(op)).rejects.toMatchObject({ outcomeUnknown: true });
  });
});
