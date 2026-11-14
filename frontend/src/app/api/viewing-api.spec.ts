import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ViewingApi } from './viewing-api';
import { ApiTransport } from './api-transport';
import { ViewingOperation } from '../models/viewing';
const id = '11111111-1111-1111-1111-111111111111';
const row = {
  id,
  unitId: id,
  projectId: id,
  status: 'PendingConfirmation',
  currentProposedStartUtc: '2026-11-08T10:00:00Z',
  currentProposedEndUtc: '2026-11-08T11:00:00Z',
  timeZoneId: 'Africa/Cairo',
  requestedAtUtc: '2026-11-07T10:00:00Z',
  pendingCycleStartedAtUtc: '2026-11-07T10:00:00Z',
  pendingExpiresAtUtc: '2026-11-08T10:00:00Z',
  version: '9223372036854775806',
  confirmedStartUtc: null,
  confirmedEndUtc: null,
};
describe('AC-04/06/09 viewing wire privacy and receipt recovery', () => {
  const transport = { get: vi.fn(), post: vi.fn() };
  let api: ViewingApi;
  beforeEach(() => {
    vi.resetAllMocks();
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: transport }] });
    api = TestBed.inject(ViewingApi);
  });
  it('projects only13 own fields, including on list, keeping exact version', async () => {
    transport.get.mockResolvedValue({
      items: [{ ...row, customerId: id, coordinationSource: 'private', outcomeNoteId: id }],
      total: 1,
      page: 1,
      pageSize: 20,
    });
    const result = await api.list(false);
    expect(result.items[0]).toEqual(row);
    expect(Object.keys(result.items[0])).toHaveLength(13);
    expect(transport.get).toHaveBeenCalledWith('/api/viewings/mine?page=1&pageSize=20');
  });
  it('drops private reasons/actors from own history even if present on wire', async () => {
    const event = {
      id,
      viewingId: id,
      viewingVersion: row.version,
      kind: 'Cancelled',
      atUtc: row.requestedAtUtc,
      proposedStartUtc: row.currentProposedStartUtc,
      proposedEndUtc: row.currentProposedEndUtc,
      confirmedStartUtc: null,
      confirmedEndUtc: null,
    };
    transport.get.mockResolvedValue({
      items: [{ ...event, reason: 'private', actorAccountId: id }],
      total: 1,
      page: 1,
      pageSize: 20,
    });
    expect((await api.history(false, id)).items[0]).toEqual(event);
  });
  it('uses current scoped staff URL and filters explicitly', async () => {
    transport.get.mockResolvedValue({ ...row, customerId: id });
    await api.detail(true, id, id);
    expect(transport.get).toHaveBeenCalledWith(`/api/crm/customers/${id}/viewings/${id}`);
    await expect(api.detail(true, id, '../accounts')).rejects.toMatchObject({ kind: 'invalid' });
  });
  it('preserves the reviewed key/body and treats malformed committed reply as uncertain', async () => {
    const op: ViewingOperation = {
      action: 'cancel',
      staff: false,
      id,
      key: 'owned-key',
      revision: 0,
      body: { version: row.version, reason: 'reviewed' },
    };
    transport.post.mockResolvedValue({
      id,
      version: '9223372036854775807',
      isReplay: true,
      private: 'drop',
    });
    expect(await api.execute(op)).toEqual({ id, version: '9223372036854775807', isReplay: true });
    expect(transport.post).toHaveBeenCalledExactlyOnceWith(
      `/api/viewings/${id}/cancel`,
      op.body,
      true,
      op.key,
    );
    transport.post.mockResolvedValue({ id, version: 1, isReplay: false });
    await expect(api.execute(op)).rejects.toMatchObject({ outcomeUnknown: true });
  });
  it('never routes a Customer confirmation command through the staff API', async () => {
    await expect(
      api.execute({ action: 'confirm', staff: false, id, key: 'key', revision: 0, body: {} }),
    ).rejects.toMatchObject({ kind: 'invalid' });
    expect(transport.post).not.toHaveBeenCalled();
  });
});
