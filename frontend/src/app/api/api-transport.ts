import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { firstValueFrom, takeUntil } from 'rxjs';
import { ApiFailure, ApiFailureKind, ApiField, API_FIELD_LABELS } from '../models/api-failure';
import { SessionContext } from '../services/session-context.service';

@Injectable({ providedIn: 'root' })
export class ApiTransport {
  private readonly http = inject(HttpClient);
  private readonly context = inject(SessionContext);
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private token: { revision: number; value: string } | null = null;
  private csrf: { revision: number; promise: Promise<void> } | null = null;

  get<T>(path: string, sessionRequired = true): Promise<T> {
    return this.send<T>('GET', path, undefined, sessionRequired);
  }
  async post<T>(
    path: string,
    body: unknown,
    sessionRequired = true,
    idempotencyKey?: string,
  ): Promise<T> {
    return this.mutate('POST', path, body, sessionRequired, idempotencyKey);
  }
  async put<T>(
    path: string,
    body: unknown,
    sessionRequired = true,
    idempotencyKey?: string,
  ): Promise<T> {
    return this.mutate('PUT', path, body, sessionRequired, idempotencyKey);
  }
  private async mutate<T>(
    method: 'POST' | 'PUT',
    path: string,
    body: unknown,
    sessionRequired: boolean,
    idempotencyKey?: string,
  ): Promise<T> {
    const revision = this.context.revision();
    await this.refreshCsrf(false);
    if (revision !== this.context.revision()) throw new ApiFailure('stale');
    return this.send<T>(method, path, body, sessionRequired, this.token!.value, idempotencyKey);
  }

  refreshCsrf(force = true): Promise<void> {
    const revision = this.context.revision();
    if (!force && this.token?.revision === revision) return Promise.resolve();
    if (this.csrf?.revision === revision) return this.csrf.promise;
    this.token = null;
    const promise = this.send<{ token: string; headerName: string }>(
      'GET',
      '/api/session/antiforgery',
      undefined,
      false,
    )
      .then((pair) => {
        if (revision !== this.context.revision()) throw new ApiFailure('stale');
        if (pair.headerName !== 'X-CSRF-TOKEN' || typeof pair.token !== 'string' || !pair.token)
          throw new ApiFailure('unknown');
        this.token = { revision, value: pair.token };
      })
      .finally(() => {
        if (this.csrf?.promise === promise) this.csrf = null;
      });
    this.csrf = { revision, promise };
    return promise;
  }

  private async send<T>(
    method: 'GET' | 'POST' | 'PUT',
    path: string,
    body: unknown,
    sessionRequired: boolean,
    token?: string,
    idempotencyKey?: string,
  ): Promise<T> {
    const revision = this.context.revision();
    const unsafe = method !== 'GET';
    // Account/private calls never execute during SSR or target a different origin.
    if (!this.browser || !/^\/api\/[a-z0-9][a-z0-9/?=&%._+-]*$/i.test(path) || path.includes('..'))
      throw new ApiFailure('invalid');
    if (sessionRequired && this.context.status() !== 'authenticated')
      throw new ApiFailure('unauthorized');
    try {
      const value = await firstValueFrom(
        this.http
          .request<T>(method, path, {
            body,
            withCredentials: true,
            transferCache: false,
            headers: token
              ? new HttpHeaders({
                  'X-CSRF-TOKEN': token,
                  ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
                })
              : undefined,
          })
          .pipe(takeUntil(this.context.changes)),
      );
      if (revision !== this.context.revision()) throw new ApiFailure('stale', [], unsafe);
      return value;
    } catch (error: unknown) {
      if (revision !== this.context.revision()) throw new ApiFailure('stale', [], unsafe);
      if (error instanceof ApiFailure) throw error;
      if (error instanceof HttpErrorResponse) {
        if (error.status === 400) this.token = null;
        if (error.status === 401 && sessionRequired) this.context.clear('expired');
        const kinds: Record<number, ApiFailureKind> = {
          0: 'network',
          400: 'invalid',
          401: 'unauthorized',
          403: 'forbidden',
          404: 'not-found',
          409: 'conflict',
          413: 'too-large',
          423: 'locked',
          503: 'unavailable',
        };
        const errors: unknown = error.error?.errors;
        const fields: ApiField[] =
          errors && typeof errors === 'object'
            ? Object.keys(errors).map((key) => {
                const field = key.toLowerCase().replace(/^\$\./, '');
                const catalogField = Object.keys(API_FIELD_LABELS).find(
                  (name) =>
                    field === name.toLowerCase() ||
                    field.startsWith(name.toLowerCase() + '[') ||
                    field.startsWith(name.toLowerCase() + '.'),
                );
                if (catalogField) return catalogField as ApiField;
                return field.includes('email')
                  ? 'email'
                  : field.includes('password')
                    ? 'password'
                    : field.includes('phone')
                      ? 'phoneNumber'
                      : field === 'role'
                        ? 'role'
                        : 'form';
              })
            : [];
        throw new ApiFailure(
          kinds[error.status] ?? 'unknown',
          [...new Set(fields)],
          unsafe && (error.status === 0 || error.status >= 500),
        );
      }
      throw new ApiFailure('unknown', [], unsafe);
    }
  }
}
