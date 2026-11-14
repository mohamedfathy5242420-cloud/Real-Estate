import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';

interface AccountDto {
  accountId: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly transport = inject(ApiTransport);
  refreshCsrf(): Promise<void> {
    return this.transport.refreshCsrf();
  }
  me(): Promise<AccountDto> {
    return this.transport.get('/api/accounts/me', false);
  }
  login(email: string, password: string, rememberMe: boolean): Promise<AccountDto> {
    return this.post('login', { email, password, rememberMe });
  }
  register(email: string, password: string, phoneNumber: string): Promise<{ message: string }> {
    return this.post('register', { email, password, phoneNumber });
  }
  confirmEmail(accountId: string, token: string): Promise<void> {
    return this.post('confirm-email', { accountId, token });
  }
  logout(): Promise<void> {
    return this.post('logout', {});
  }
  forgotPassword(email: string): Promise<{ message: string }> {
    return this.post('forgot-password', { email });
  }
  resetPassword(email: string, token: string, newPassword: string): Promise<void> {
    return this.post('reset-password', { email, token, newPassword });
  }
  resendConfirmation(email: string): Promise<{ message: string }> {
    return this.post('resend-confirmation', { email });
  }
  private async post<T = void>(action: string, body: unknown): Promise<T> {
    const result = await this.transport.post<T>(`/api/accounts/${action}`, body, false);
    if (action === 'login' || action === 'logout' || action === 'reset-password')
      await this.transport.refreshCsrf();
    return result;
  }
}
