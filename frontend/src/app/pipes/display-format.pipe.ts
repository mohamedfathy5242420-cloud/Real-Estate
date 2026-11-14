import { Pipe, PipeTransform } from '@angular/core';

const numberFormatter = new Intl.NumberFormat('en-US', {
  numberingSystem: 'latn',
  maximumFractionDigits: 2,
});

@Pipe({ name: 'displayNumber' })
export class DisplayNumberPipe implements PipeTransform {
  transform(value: number | string | null): string {
    if (value === null) return 'Not specified';
    // Intl accepts exact decimal strings at runtime. This assertion performs no Number conversion.
    return numberFormatter.format(value as number);
  }
}

@Pipe({ name: 'displayCurrency' })
export class DisplayCurrencyPipe implements PipeTransform {
  transform(amount: number | string, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      numberingSystem: 'latn',
      style: 'currency',
      currency,
      currencyDisplay: 'name',
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(amount as number);
  }
}
