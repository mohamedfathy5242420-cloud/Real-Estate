import { TestBed } from '@angular/core/testing';
import { AccountApi } from '../api/account-api';
import { AccountService } from './account.service';

describe('BR-04 / FAC-02 account display projection', () => {
  it('drops unexpected account data and does not retain aliases to the wire role array', async () => {
    const dto = {
      accountId: 'own',
      email: 'own@brokerhub.test',
      roles: ['Customer'],
      internalNotes: 'private-note-sentinel',
      passwordHash: 'private-hash-sentinel',
    };
    TestBed.configureTestingModule({
      providers: [
        { provide: AccountApi, useValue: { login: async () => dto, me: async () => dto } },
      ],
    });
    const service = TestBed.inject(AccountService);
    const account = await service.login(dto.email, 'unused-local-test-password', false);
    expect(account).toEqual({ accountId: 'own', email: 'own@brokerhub.test', roles: ['Customer'] });
    expect(JSON.stringify(await service.me())).not.toContain('private-');
    dto.roles.push('SystemAdministrator');
    expect(account.roles).toEqual(['Customer']);
  });
});
