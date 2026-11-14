import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { CrmApi } from './crm-api';
import { ApiTransport } from './api-transport';
import { ApiFailure } from '../models/api-failure';
import { CrmCustomer, CrmOperation, EmployeeProfile } from '../models/crm';
const id = '11111111-1111-1111-1111-111111111111',
  other = '22222222-2222-2222-2222-222222222222';
const profile = {
  id,
  fullName: 'Fictional',
  phone: '+201000000000',
  email: null,
  preferredContactMethod: null,
  requirements: null,
  version: '9223372036854775807',
  createdAtUtc: '2026-11-05T00:00:00Z',
  updatedAtUtc: null,
};
describe('AC-04/05 approved CRM wire and receipt boundaries', () => {
  let api: CrmApi;
  const transport = { get: vi.fn(), post: vi.fn(), put: vi.fn() };
  beforeEach(() => {
    vi.resetAllMocks();
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: transport }] });
    api = TestBed.inject(CrmApi);
  });
  it('projects own Customer data without assignment, account or staff history fields', async () => {
    transport.get.mockResolvedValue({
      ...profile,
      accountId: other,
      assignedEmployeeId: other,
      notes: [{ body: 'private' }],
    });
    expect(await api.own()).toEqual(profile);
  });
  it('treats only own404 as onboarding and keeps resource403 distinct', async () => {
    transport.get
      .mockRejectedValueOnce(new ApiFailure('not-found'))
      .mockRejectedValueOnce(new ApiFailure('forbidden'));
    expect(await api.own()).toBeNull();
    await expect(api.own()).rejects.toMatchObject({ kind: 'forbidden' });
  });
  it('sends only contact fields and quoted Version on own update', async () => {
    transport.put.mockResolvedValue(profile);
    await api.saveOwn(profile, {
      fullName: ' Fictional ',
      phone: ' 123 ',
      email: '',
      preferredContactMethod: '',
      requirements: '',
    });
    expect(transport.put).toHaveBeenCalledWith('/api/customers/' + id, {
      fullName: 'Fictional',
      phone: '123',
      email: null,
      preferredContactMethod: null,
      requirements: null,
      version: profile.version,
    });
  });
  it('does not resend immutable Employee AccountId/number on update', async () => {
    const employee = {
      id,
      accountId: other,
      employeeNumber: 'E1',
      displayName: 'Employee',
      isActive: true,
      version: '9',
      createdAtUtc: '',
      createdByAccountId: other,
      updatedAtUtc: null,
      updatedByAccountId: null,
    };
    transport.put.mockResolvedValue(employee);
    await api.saveEmployee(employee as EmployeeProfile, {
      accountId: id,
      employeeNumber: 'CHANGED',
      displayName: ' New ',
      isActive: false,
    });
    expect(transport.put).toHaveBeenCalledWith('/api/employees/' + id, {
      displayName: 'New',
      isActive: false,
      version: '9',
    });
  });
  it('projects paged follow-ups and drops extra fields from each row', async () => {
    transport.get.mockResolvedValue({
      items: [{ ...profile, assignedEmployeeId: other, accountId: 'private' }],
      page: 2,
      pageSize: 20,
      total: 21,
      secret: 'private',
    });
    const result = await api.followUps(2, { hasNotes: 'true' });
    expect(transport.get).toHaveBeenCalledWith(
      '/api/crm/follow-ups?page=2&pageSize=20&hasNotes=true',
    );
    expect(result.items[0]).toEqual({ ...profile, assignedEmployeeId: other });
    expect(Object.keys(result)).toEqual(['items', 'page', 'pageSize', 'total']);
  });
  it('keeps command body and key exact for explicit receipt recovery', async () => {
    const operation: CrmOperation = {
      action: 'needs',
      customerId: id,
      command: {
        key: 'owned-receipt-1',
        body: { version: profile.version, requirements: null },
        revision: 0,
      },
    };
    transport.put.mockRejectedValueOnce(new ApiFailure('network', [], true)).mockResolvedValueOnce({
      id: other,
      customerId: id,
      version: '1',
      isReplay: true,
      secret: 'dropped',
    });
    await expect(api.execute(operation)).rejects.toMatchObject({ outcomeUnknown: true });
    const receipt = await api.execute(operation);
    expect(transport.put).toHaveBeenCalledTimes(2);
    expect(transport.put.mock.calls[0]).toEqual(transport.put.mock.calls[1]);
    expect(transport.put.mock.calls[1]).toEqual([
      '/api/crm/customers/' + id + '/requirements',
      operation.command.body,
      true,
      operation.command.key,
    ]);
    expect('secret' in receipt).toBe(false);
  });
  it('does not report malformed committed replies as a safe unsaved failure', async () => {
    transport.post.mockResolvedValue(null);
    await expect(
      api.execute({ action: 'intake', command: { key: 'test', revision: 0, body: {} } }),
    ).rejects.toMatchObject({ kind: 'unknown', outcomeUnknown: true });
    transport.put.mockResolvedValue({ ...profile, version: 1 });
    await expect(
      api.saveOwn(profile, {
        fullName: 'a',
        phone: 'b',
        email: '',
        preferredContactMethod: '',
        requirements: '',
      }),
    ).rejects.toMatchObject({ outcomeUnknown: true });
  });
  it('does not place invalid customer IDs into a protected path', async () => {
    await expect(api.customer('../employees')).rejects.toMatchObject({ kind: 'invalid' });
    expect(transport.get).not.toHaveBeenCalled();
  });
});
