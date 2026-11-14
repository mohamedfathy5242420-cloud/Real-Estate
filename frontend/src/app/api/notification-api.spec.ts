import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ApiTransport } from './api-transport';
import { NotificationApi, notificationProjection } from './notification-api';
const id = '11111111-1111-1111-1111-111111111111';
const utc = '2026-11-08T00:00:00.1234567+00:00';
const row = {
  id,
  kind: 'Viewing.Confirmed',
  title: 'Confirm viewing',
  message: 'Confirmed',
  occurredAtUtc: utc,
  createdAtUtc: utc,
  readAtUtc: null,
  resource: {
    kind: 'Viewing',
    id,
    customerId: id,
    parentId: null,
    relatedId: null,
    internalNotes: 'private',
  },
  internalNotes: 'private',
  recipientEmail: 'private',
};
describe('AC-09/14 notification wire privacy and natural read idempotency', () => {
  it('selects only safe DTO8/resource5 fields and preserves original UTC fractions', () => {
    const result = notificationProjection(row);
    expect(Object.keys(result)).toHaveLength(8);
    expect(Object.keys(result.resource)).toHaveLength(5);
    expect(JSON.stringify(result)).not.toContain('private');
    expect(result.occurredAtUtc).toBe(utc);
    expect(() => notificationProjection({ ...row, readAtUtc: '2026-11-08T00:00:00' })).toThrow();
  });
  it('uses totalCount and omits the all filter; rejects literal all rather than sending it', async () => {
    const get = vi.fn().mockResolvedValue({ items: [row], totalCount: 21, page: 2, pageSize: 20 });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { get } }] });
    const api = TestBed.inject(NotificationApi);
    expect((await api.list(2)).totalCount).toBe(21);
    expect(get).toHaveBeenCalledWith('/api/notifications?page=2&pageSize=20');
    await api.list(2, 'unread');
    expect(get).toHaveBeenLastCalledWith('/api/notifications?page=2&pageSize=20&readStatus=unread');
    await expect(api.list(1, 'all' as never)).rejects.toThrow();
    expect(get).toHaveBeenCalledTimes(2);
  });
  it('sends no mark-read body/key/version and validates the returned original notification', async () => {
    const put = vi.fn().mockResolvedValue({ notificationId: id, readAtUtc: utc });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { put } }] });
    const api = TestBed.inject(NotificationApi);
    expect(await api.markRead(id)).toEqual({ notificationId: id, readAtUtc: utc });
    expect(put).toHaveBeenCalledWith(`/api/notifications/${id}/read`, undefined);
    put.mockResolvedValue({
      notificationId: '22222222-2222-2222-2222-222222222222',
      readAtUtc: utc,
    });
    await expect(api.markRead(id)).rejects.toMatchObject({ kind: 'unknown', outcomeUnknown: true });
    put.mockResolvedValue({ notificationId: id, readAtUtc: 'invalid' });
    await expect(api.markRead(id)).rejects.toMatchObject({ kind: 'unknown', outcomeUnknown: true });
  });
  it('rejects quoted or out-of-range unread counts rather than showing a false zero', async () => {
    const get = vi.fn().mockResolvedValue({ unreadCount: '1' });
    TestBed.configureTestingModule({ providers: [{ provide: ApiTransport, useValue: { get } }] });
    await expect(TestBed.inject(NotificationApi).count()).rejects.toThrow();
    get.mockResolvedValue({ unreadCount: 2147483648 });
    await expect(TestBed.inject(NotificationApi).count()).rejects.toThrow();
  });
});
