import { firstValueFrom } from 'rxjs';
import { DemoCatalogReader } from './demo-catalog-reader';

describe('DemoCatalogReader', () => {
  it('finds fictional public samples by location without case or surrounding space sensitivity', async () => {
    const result = await firstValueFrom(new DemoCatalogReader().search({ text: '  NEW CAIRO  ' }));
    expect(result.total).toBe(3);
    expect(result.items.every((item) => item.location === 'New Cairo')).toBe(true);
    expect(result.items.every((item) => item.id.startsWith('demo-'))).toBe(true);
  });

  it('supports all samples, project text and a successful no-match result', async () => {
    const reader = new DemoCatalogReader();
    expect((await firstValueFrom(reader.search({ text: '' }))).total).toBe(6);
    expect((await firstValueFrom(reader.search({ text: 'Cedar' }))).total).toBe(2);
    expect(await firstValueFrom(reader.search({ text: 'no-such-demo-unit' }))).toEqual({
      items: [],
      total: 0,
    });
  });
});

describe('English Demo search', () => {
  it('finds English locations and project names without translating IDs or numeric prices', async () => {
    const reader = new DemoCatalogReader();
    const result = await firstValueFrom(reader.search({ text: ' New Cairo ' }));
    expect(result.total).toBe(3);
    expect((await firstValueFrom(reader.search({ text: 'Cedar' }))).total).toBe(2);
    expect(result.items[0].id).toBe('demo-cedar-01');
    expect(result.items[0].price).toEqual({ amount: 6400000, currency: 'EGP' });
  });
});

describe('Demo unit detail reader', () => {
  it('returns matching public details without requiring a session', async () => {
    const unit = await firstValueFrom(new DemoCatalogReader().getById('demo-cedar-01'));
    expect(unit?.title).toBe('Garden apartment');
    expect(unit?.description).toContain('demo');
    expect(unit?.features).toContain('Garden views');
    expect(unit?.price).toEqual({ amount: 6400000, currency: 'EGP' });
  });

  it('returns null for absent IDs without converting opaque IDs or returning the first sample', async () => {
    const reader = new DemoCatalogReader();
    expect(await firstValueFrom(reader.getById('unknown-unit'))).toBeNull();
    expect(await firstValueFrom(reader.getById('DEMO-CEDAR-01'))).toBeNull();
    expect(await firstValueFrom(reader.getById('<script>alert(1)</script>'))).toBeNull();
  });
});
