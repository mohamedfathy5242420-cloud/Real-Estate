import { AccountService } from './services/account.service';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideDemoCatalog } from './api/demo-catalog-reader';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: AccountService,
          useValue: {
            start: () => undefined,
            account: () => null,
            ensure: async () => ({
              accountId: 'test',
              email: 'test@brokerhub.test',
              roles: ['Customer', 'SalesEmployee', 'SalesManager', 'SystemAdministrator'],
            }),
          },
        },
        provideRouter(routes),
        ...provideDemoCatalog(),
      ],
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
    expect(compiled.querySelector('nav')?.getAttribute('aria-label')).toBe('Main navigation');
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

  it('should expose one exact current navigation link', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/catalog');
    await fixture.whenStable();
    fixture.detectChanges();

    const catalog = fixture.nativeElement.querySelector(
      '#primary-nav a[href="/catalog"]',
    ) as HTMLAnchorElement;
    const unit = fixture.nativeElement.querySelector(
      'a[href="/auth/session"]',
    ) as HTMLAnchorElement;

    expect(catalog.getAttribute('aria-current')).toBe('page');
    expect(unit.getAttribute('aria-current')).toBeNull();
  });
});
