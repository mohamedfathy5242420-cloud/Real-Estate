import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { CatalogAdminService } from './catalog-admin.service';
import { CatalogAdminApi, catalogProjection } from '../api/catalog-admin-api';
const id = '11111111-1111-1111-1111-111111111111';
describe('FAC-01 management command construction', () => {
  it('does not resend immutable parent/code or convert exact financial strings on unit update', async () => {
    const save = vi.fn().mockResolvedValue(null);
    TestBed.configureTestingModule({
      providers: [CatalogAdminService, { provide: CatalogAdminApi, useValue: { save } }],
    });
    const unit = catalogProjection('units', {
      id,
      version: '9223372036854775807',
      areaSqm: '1.00',
      listPrice: '0.00',
    });
    await TestBed.inject(CatalogAdminService).save(
      'units',
      unit,
      {
        projectId: 'untrusted-parent',
        code: 'replacement',
        title: 'Unit',
        description: '',
        unitType: 'Apartment',
        areaSqm: '150.25',
        bedrooms: '',
        bathrooms: '0',
        listPrice: '9999999999999999.99',
        currency: 'egp',
      },
      [],
      [],
    );
    const body = save.mock.calls[0][2];
    expect(body).not.toHaveProperty('code');
    expect(body).not.toHaveProperty('projectId');
    expect(body.listPrice).toBe('9999999999999999.99');
    expect(body.bedrooms).toBeNull();
    expect(body.bathrooms).toBe(0);
  });
  it('normalizes optional confirmation facts without inventing availability', async () => {
    const confirm = vi.fn().mockResolvedValue(null);
    TestBed.configureTestingModule({
      providers: [CatalogAdminService, { provide: CatalogAdminApi, useValue: { confirm } }],
    });
    const unit = catalogProjection('units', {
      id,
      version: '1',
      areaSqm: '1.00',
      listPrice: '0.00',
    });
    if (unit.kind !== 'units') throw new Error('test fixture');
    await TestBed.inject(CatalogAdminService).confirm(unit, {
      confirmedAtUtc: '2026-11-05T08:00:00Z',
      source: 'Developer',
      sourceReference: '',
      notes: '',
      confirmedPrice: '0.00',
      currency: 'egp',
      availabilityStatus: '',
    });
    expect(confirm.mock.calls[0][1]).toMatchObject({
      confirmedPrice: '0.00',
      currency: 'EGP',
      availabilityStatus: null,
    });
  });
});
