import { CatalogFilters } from './catalog';
export function validateCatalogFilters(v: CatalogFilters): string {
  const guid = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i;
  if ((v.area?.length ?? 0) > 200 || (v.unitType?.length ?? 0) > 100)
    return 'Shorten the area or unit type.';
  if (
    [v.projectId, v.developerId].some(
      (value) => value && (!guid.test(value) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(value)),
    )
  )
    return 'Invalid project or developer selection.';
  if (v.sort && !['newest', 'priceAsc', 'priceDesc', 'areaAsc', 'areaDesc'].includes(v.sort))
    return 'Choose a sort option from the list.';
  if (v.currency && !/^[A-Za-z]{3}$/.test(v.currency))
    return 'Currency must contain three English letters.';
  if ((v.minPrice || v.maxPrice || v.sort === 'priceAsc' || v.sort === 'priceDesc') && !v.currency)
    return 'Choose a currency before filtering or sorting by price; currencies are kept separate.';
  if (v.bedrooms && (!/^\d+$/.test(v.bedrooms) || Number(v.bedrooms) > 2147483647))
    return 'Bedrooms must be a nonnegative integer.';
  if (
    v.pageSize &&
    (!/^\d+$/.test(v.pageSize) || Number(v.pageSize) < 1 || Number(v.pageSize) > 100)
  )
    return 'Page size must be between 1 and 100.';
  const scaled = (value: string) => {
    const [whole, fraction = ''] = value.split('.');
    return BigInt(whole) * 100n + BigInt(fraction.padEnd(2, '0'));
  };
  for (const [min, max, area] of [
    [v.minPrice, v.maxPrice, false],
    [v.minArea, v.maxArea, true],
  ] as const) {
    for (const value of [min, max])
      if (value && (!/^\d{1,16}(?:\.\d{1,2})?$/.test(value) || (area && scaled(value) === 0n)))
        return 'Review price and area limits: use Western digits and up to two decimal places; area must be positive.';
    if (min && max && scaled(min) > scaled(max)) return 'The minimum cannot exceed the maximum.';
  }
  return '';
}
