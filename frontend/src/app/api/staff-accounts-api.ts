import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { ApiFailure } from '../models/api-failure';
import { STAFF_ROLES } from '../models/role-navigation';
import { StaffAccountInput, StaffAccountCreated } from '../models/staff-account';

@Injectable({ providedIn: 'root' })
export class StaffAccountsApi {
  private readonly transport = inject(ApiTransport);
  async create(input: StaffAccountInput): Promise<StaffAccountCreated> {
    if (!STAFF_ROLES.includes(input.role)) throw new ApiFailure('invalid', ['role']);
    const value = await this.transport.post<StaffAccountCreated>('/api/staff-accounts', {
      email: input.email,
      password: input.password,
      phoneNumber: input.phoneNumber,
      role: input.role,
    });
    if (
      !value ||
      typeof value.id !== 'string' ||
      typeof value.email !== 'string' ||
      !STAFF_ROLES.includes(value.role)
    )
      throw new ApiFailure('unknown', [], true);
    return Object.freeze({ id: value.id, email: value.email, role: value.role });
  }
}
