import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ApiTransport } from './api-transport';
import { CatalogAdminApi, catalogProjection } from './catalog-admin-api';
import { AdminUnit } from '../models/catalog-admin';
const unitId = '11111111-1111-1111-1111-111111111111';
const dto = {
  id: unitId,
  version: '9223372036854775807',
  areaSqm: '9999999999999999.99',
  listPrice: '9999999999999999.99',
  title: 'Test',
  createdAtUtc: '2026-11-05T00:00:00Z',
  updatedAtUtc: null,
  internalNotes: 'private-sentinel',
  storageKey: 'private-sentinel',
};
describe('BR-01/02/03/14 exact management contracts', () => {
  it('projects only named fields and rejects numeric versions or monetary values', () => {
    const result = catalogProjection('units', dto) as AdminUnit;
    expect(result.version).toBe('9223372036854775807');
    expect(result.listPrice).toBe('9999999999999999.99');
    expect(JSON.stringify(result)).not.toContain('private-sentinel');
    expect(() => catalogProjection('units', { ...dto, version: 1 })).toThrow();
    expect(() => catalogProjection('units', { ...dto, listPrice: 1 })).toThrow();
  });
  it('uses nested unit mutation payloads and preserves the exact outgoing Version', async () => {
    const post = vi.fn();
    post.mockResolvedValue({ unit: dto, transition: { private: 'ignored' } });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { post } }] });
    const api = TestBed.inject(CatalogAdminApi);
    const unit = catalogProjection('units', dto) as AdminUnit;
    const result = await api.publication(unit, true, ' Reason ');
    expect(post).toHaveBeenCalledExactlyOnceWith(`/api/units/${unitId}/publish`, {
      version: '9223372036854775807',
      reason: 'Reason',
    });
    expect(result.id).toBe(unitId);
    expect(JSON.stringify(result)).not.toContain('transition');
  });
  it('uses declared multipart fields only, never number-coercing Version', async () => {
    const post = vi.fn();
    post.mockResolvedValue({ unitVersion: '2', image: { id: unitId } });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { post } }] });
    const api = TestBed.inject(CatalogAdminApi);
    const unit = catalogProjection('units', dto) as AdminUnit;
    const file = new File(['fake decoded by server test elsewhere'], 'test.png', {
      type: 'image/png',
    });
    await api.upload(unit, file, 'Description', 3);
    const body = post.mock.calls[0][1] as FormData;
    expect([...body.keys()]).toEqual(['File', 'AltText', 'SortOrder', 'Version']);
    expect(body.get('Version')).toBe('9223372036854775807');
  });
  it('keeps agreement rules minimal and copies nested arrays instead of retaining wire aliases', () => {
    const original = {
      id: unitId,
      version: '1',
      projectIds: [unitId],
      commissionRules: [
        {
          projectId: null,
          ruleType: 'Percentage',
          percentage: '2.5001',
          fixedAmount: null,
          currency: 'EGP',
          secret: 'private-sentinel',
        },
      ],
    };
    const result = catalogProjection('agreements', original);
    original.projectIds.push('other');
    original.commissionRules[0].secret = 'changed';
    expect(JSON.stringify(result)).not.toContain('private-sentinel');
    expect(JSON.stringify(result)).not.toContain('changed');
    if (result.kind === 'agreements') expect(result.projectIds).toEqual([unitId]);
  });
});
