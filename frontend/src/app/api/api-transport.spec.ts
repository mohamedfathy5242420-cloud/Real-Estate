import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiTransport } from './api-transport';
import { SessionContext } from '../services/session-context.service';

const settle = async () => {
  for (let n = 0; n < 12; n++) await Promise.resolve();
};
describe('BR-04/06 session transport isolation', () => {
  let http: HttpTestingController;
  let scope: SessionContext;
  let api: ApiTransport;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    scope = TestBed.inject(SessionContext);
    api = TestBed.inject(ApiTransport);
    scope.accept(scope.revision(), {
      accountId: 'admin',
      email: 'admin@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
  });
  afterEach(() => http.verify());
  it('sends an explicit CRM receipt key alongside csrf without replaying the request', async () => {
    const pending = api.put(
      '/api/crm/customers/test/requirements',
      { version: '9', requirements: null },
      true,
      'owned-receipt',
    );
    http.expectOne('/api/session/antiforgery').flush({ token: 'csrf', headerName: 'X-CSRF-TOKEN' });
    await settle();
    const request = http.expectOne('/api/crm/customers/test/requirements');
    expect(request.request.headers.get('Idempotency-Key')).toBe('owned-receipt');
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('csrf');
    request.flush({ isReplay: false });
    await pending;
    http.expectNone('/api/crm/customers/test/requirements');
  });
  it('cancels an old private response on identity change without exposing it to the next account', async () => {
    const old = api.get('/api/private-test').catch((error) => error);
    const pending = http.expectOne('/api/private-test');
    const revision = scope.clear('loading');
    scope.accept(revision, {
      accountId: 'customer',
      email: 'own@brokerhub.test',
      roles: ['Customer'],
    });
    expect(pending.cancelled).toBe(true);
    expect(await old).toMatchObject({ kind: 'stale' });
    expect(scope.account()?.accountId).toBe('customer');
  });
  it('clears private identity on401 but keeps an authenticated user on resource403', async () => {
    const denied = api.get('/api/private-test').catch((error) => error);
    http.expectOne('/api/private-test').flush(null, { status: 403, statusText: 'Forbidden' });
    expect(await denied).toMatchObject({ kind: 'forbidden' });
    expect(scope.account()).not.toBeNull();
    const expired = api.get('/api/private-test').catch((error) => error);
    http.expectOne('/api/private-test').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(await expired).toMatchObject({ kind: 'unauthorized' });
    expect(scope.status()).toBe('expired');
    expect(scope.account()).toBeNull();
  });
  it('shares one csrf bootstrap and preserves lossless strings without retrying unknown writes', async () => {
    const first = api.post('/api/test-a', {
      amount: '9999999999999999.99',
      version: '9223372036854775807',
    });
    const second = api.post('/api/test-b', {});
    const result = first.catch((error) => error);
    http
      .expectOne('/api/session/antiforgery')
      .flush({ token: 'owned', headerName: 'X-CSRF-TOKEN' });
    await settle();
    const a = http.expectOne('/api/test-a');
    expect(a.request.body.amount).toBe('9999999999999999.99');
    expect(a.request.body.version).toBe('9223372036854775807');
    expect(a.request.withCredentials).toBe(true);
    expect(a.request.transferCache).toBe(false);
    expect(a.request.headers.get('X-CSRF-TOKEN')).toBe('owned');
    a.error(new ProgressEvent('error'));
    http.expectOne('/api/test-b').flush(null);
    await second;
    expect(await result).toMatchObject({ kind: 'network', outcomeUnknown: true });
    http.expectNone('/api/test-a');
  });
  it('never sends an unsafe request when its csrf bootstrap belongs to an old identity', async () => {
    const pending = api.post('/api/test-a', {}).catch((error) => error);
    const pair = http.expectOne('/api/session/antiforgery');
    scope.clear('guest');
    expect(pair.cancelled).toBe(true);
    expect(await pending).toMatchObject({ kind: 'stale' });
    http.expectNone('/api/test-a');
  });
  it('never attaches session credentials to an external URL', async () => {
    await expect(api.get('https://hostile.invalid/api/test')).rejects.toMatchObject({
      kind: 'invalid',
    });
    http.expectNone(() => true);
  });
});
describe('anonymous public SSR', () => {
  it('does not initialize account or csrf HTTP on the server', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'server' },
      ],
    });
    await expect(TestBed.inject(ApiTransport).get('/api/accounts/me', false)).rejects.toMatchObject(
      { kind: 'invalid' },
    );
    TestBed.inject(HttpTestingController).expectNone(() => true);
  });
});
