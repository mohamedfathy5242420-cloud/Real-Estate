import { DisplayCurrencyPipe, DisplayNumberPipe } from './display-format.pipe';

describe('Lossless wire decimal display', () => {
  it('does not round maximum decimal(18,2) money through a JavaScript Number', () => {
    expect(new DisplayCurrencyPipe().transform('9999999999999999.99', 'EGP')).toContain(
      '9,999,999,999,999,999.99',
    );
    expect(new DisplayNumberPipe().transform('123.45')).toBe('123.45');
    expect(new DisplayNumberPipe().transform(null)).toBe('Not specified');
  });
});
