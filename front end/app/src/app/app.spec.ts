import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the accessible BrokerHub shell', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('BrokerHub');
    expect(compiled.querySelector('main#main-content')).toBeTruthy();
    expect(compiled.querySelector('nav')?.getAttribute('aria-label')).toBe('Primary navigation');
  });

  it('should expose the mobile navigation state', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('.nav-toggle') as HTMLButtonElement;
    const navigation = fixture.nativeElement.querySelector('#primary-nav') as HTMLElement;

    expect(button.getAttribute('aria-expanded')).toBe('false');
    expect(navigation.classList.contains('open')).toBe(false);

    button.click();
    fixture.detectChanges();

    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(navigation.classList.contains('open')).toBe(true);
  });

  it('should move focus to main content from the skip link', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('.skip-link') as HTMLAnchorElement;

    link.click();
    await fixture.whenStable();

    expect(document.activeElement?.id).toBe('main-content');
  });
});
