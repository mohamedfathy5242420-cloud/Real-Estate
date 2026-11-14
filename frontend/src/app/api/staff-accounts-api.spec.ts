import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ApiTransport } from './api-transport';
import { StaffAccountsApi } from './staff-accounts-api';
describe('FAC-11 actual staff wire boundary', () => {
  it('posts the approved payload and projects only id/email/role', async () => {
    const post = vi.fn();
    post.mockResolvedValue({
      id: 'created',
      email: 'employee@brokerhub.test',
      role: 'SalesEmployee',
      passwordHash: 'private-sentinel',
      internalNotes: 'private-sentinel',
    });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { post } }] });
    const created = await TestBed.inject(StaffAccountsApi).create({
      email: 'employee@brokerhub.test',
      password: 'not-real',
      phoneNumber: '+201000000001',
      role: 'SalesEmployee',
    });
    expect(post).toHaveBeenCalledExactlyOnceWith('/api/staff-accounts', {
      email: 'employee@brokerhub.test',
      password: 'not-real',
      phoneNumber: '+201000000001',
      role: 'SalesEmployee',
    });
    expect(created).toEqual({
      id: 'created',
      email: 'employee@brokerhub.test',
      role: 'SalesEmployee',
    });
    expect(JSON.stringify(created)).not.toContain('private-sentinel');
  });
  it('treats a malformed201 body as unknown outcome rather than success', async () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ApiTransport,
          useValue: { post: async () => ({ id: 'created', role: 'Admin' }) },
        },
      ],
    });
    await expect(
      TestBed.inject(StaffAccountsApi).create({
        email: 'employee@brokerhub.test',
        password: 'not-real',
        phoneNumber: '+201000000001',
        role: 'SalesEmployee',
      }),
    ).rejects.toMatchObject({ kind: 'unknown', outcomeUnknown: true });
  });
});
