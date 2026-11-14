import { DisplayCurrencyPipe, DisplayNumberPipe } from './display-format.pipe';

describe('English display formatting', () => {
  it('renders counts and decimal measurements with English digits and separators', () => {
    const pipe = new DisplayNumberPipe();
    expect(pipe.transform(3)).toBe('3');
    expect(pipe.transform(1234.5)).toBe('1,234.5');
    expect(pipe.transform(0)).toBe('0');
  });

  it('renders currency names in English while retaining numeric transport values', () => {
    const amount = 6400000;
    const output = new DisplayCurrencyPipe().transform(amount, 'EGP');
    expect(output).toContain('6,400,000');
    expect(output).toContain('Egyptian pounds');
    expect(output).not.toMatch(/[\u0600-\u06ff]/);
    expect(amount).toBe(6400000);
  });
});
