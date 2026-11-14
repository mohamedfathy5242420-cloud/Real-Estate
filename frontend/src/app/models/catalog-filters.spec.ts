import { validateCatalogFilters } from './catalog-filters';
describe('BR-03/16 public filters without financial rounding or currency mixing', () => {
  it('requires currency for price bounds/sorting and rejects an unsupported sort', () => {
    expect(validateCatalogFilters({ minPrice: '1' })).toContain('currency');
    expect(validateCatalogFilters({ sort: 'priceDesc' })).toContain('currency');
    expect(validateCatalogFilters({ sort: 'commission' })).toContain('sort');
  });
  it('compares the last cent of decimal18,2 losslessly', () => {
    expect(
      validateCatalogFilters({
        minPrice: '9999999999999999.98',
        maxPrice: '9999999999999999.99',
        currency: 'EGP',
      }),
    ).toBe('');
    expect(
      validateCatalogFilters({
        minPrice: '9999999999999999.99',
        maxPrice: '9999999999999999.98',
        currency: 'EGP',
      }),
    ).toContain('minimum');
  });
  it('preserves zero price/bedrooms while requiring positive area and valid paging', () => {
    expect(
      validateCatalogFilters({
        minPrice: '0',
        bedrooms: '0',
        currency: 'EGP',
        minArea: '0.01',
        pageSize: '100',
      }),
    ).toBe('');
    expect(validateCatalogFilters({ minArea: '0' })).not.toBe('');
    expect(validateCatalogFilters({ pageSize: '101' })).not.toBe('');
    expect(
      validateCatalogFilters({ developerId: '00000000-0000-0000-0000-000000000000' }),
    ).not.toBe('');
  });
  it('rejects extra decimal places instead of silently rounding', () => {
    expect(validateCatalogFilters({ maxPrice: '1.001', currency: 'EGP' })).not.toBe('');
  });
});
