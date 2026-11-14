import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AccountApi } from './account-api';

describe('Cookie-session CSRF transport', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('bootstraps before login and renews after identity changes before the next mutation', async () => {
    const api = TestBed.inject(AccountApi);
    const login = api.login('test@brokerhub.test', 'not-a-real-secret', false);
    const initial = http.expectOne('/api/session/antiforgery');
    expect(initial.request.withCredentials).toBe(true);
    expect(initial.request.transferCache).toBe(false);
    initial.flush({ token: 'anonymous', headerName: 'X-CSRF-TOKEN' });
    for (let n = 0; n < 12; n++) await Promise.resolve();
    const request = http.expectOne('/api/accounts/login');
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('anonymous');
    expect(request.request.withCredentials).toBe(true);
    request.flush({ accountId: 'account', email: 'test@brokerhub.test', roles: ['Customer'] });
    for (let n = 0; n < 12; n++) await Promise.resolve();
    http
      .expectOne('/api/session/antiforgery')
      .flush({ token: 'signed-in', headerName: 'X-CSRF-TOKEN' });
    await login;
    const logout = api.logout();
    for (let n = 0; n < 12; n++) await Promise.resolve();
    const mutation = http.expectOne('/api/accounts/logout');
    expect(mutation.request.headers.get('X-CSRF-TOKEN')).toBe('signed-in');
    mutation.flush(null, { status: 204, statusText: 'No Content' });
    for (let n = 0; n < 12; n++) await Promise.resolve();
    http
      .expectOne('/api/session/antiforgery')
      .flush({ token: 'signed-out', headerName: 'X-CSRF-TOKEN' });
    await logout;
  });

  it('never retries a mutation after a network failure or stores a session in SSR transfer cache', async () => {
    const api = TestBed.inject(AccountApi);
    const operation = api.register('test@brokerhub.test', 'not-a-real-secret', '+201000000001');
    const rejected = expect(operation).rejects.toMatchObject({ kind: 'network' });
    http
      .expectOne('/api/session/antiforgery')
      .flush({ token: 'anonymous', headerName: 'X-CSRF-TOKEN' });
    for (let n = 0; n < 12; n++) await Promise.resolve();
    http.expectOne('/api/accounts/register').error(new ProgressEvent('error'));
    await rejected;
    http.expectNone('/api/accounts/register');
    const session = api.me();
    const request = http.expectOne('/api/accounts/me');
    expect(request.request.transferCache).toBe(false);
    request.flush({ accountId: 'own', email: 'own@brokerhub.test', roles: ['Customer'] });
    await session;
  });

  it('maps validation fields to safe application errors without HTTP diagnostics', async () => {
    const operation = TestBed.inject(AccountApi).register(
      'test@brokerhub.test',
      'not-a-real-secret',
      '+201000000001',
    );
    const failure = operation.catch((error) => error);
    http
      .expectOne('/api/session/antiforgery')
      .flush({ token: 'anonymous', headerName: 'X-CSRF-TOKEN' });
    for (let n = 0; n < 12; n++) await Promise.resolve();
    http.expectOne('/api/accounts/register').flush(
      {
        detail: 'private-sql-diagnostic',
        errors: { 'private-contact-sentinel': ['hidden value'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    const error = await failure;
    expect(error.kind).toBe('invalid');
    expect(error.fields).toEqual(['form']);
    expect(JSON.stringify(error)).not.toContain('private-');
    expect(JSON.stringify(error)).not.toContain('hidden value');
  });
});
