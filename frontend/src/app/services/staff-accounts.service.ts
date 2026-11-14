import { Injectable, inject } from '@angular/core';
import { StaffAccountsApi } from '../api/staff-accounts-api';
import { StaffAccountInput } from '../models/staff-account';

@Injectable({ providedIn: 'root' })
export class StaffAccountsService {
  private readonly api = inject(StaffAccountsApi);
  create(input: StaffAccountInput) {
    return this.api.create(input);
  }
}
