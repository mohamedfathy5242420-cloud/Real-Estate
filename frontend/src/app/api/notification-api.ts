import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { crmId } from './crm-api';
import { ApiFailure } from '../models/api-failure';
import {
  NotificationFilter,
  NotificationPage,
  NotificationReadReply,
  UserNotification,
} from '../models/notification';

function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new ApiFailure('unknown');
  return value as Record<string, unknown>;
}
function text(value: unknown): string {
  if (typeof value !== 'string' || !value) throw new ApiFailure('unknown');
  return value;
}
function id(value: unknown): string {
  try {
    return crmId(text(value));
  } catch {
    throw new ApiFailure('unknown');
  }
}
function nullableId(value: unknown): string | null {
  return value === null ? null : id(value);
}
function utc(value: unknown): string {
  const result = text(value);
  if (
    !/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?(?:Z|\+00:00)$/.test(result) ||
    !Number.isFinite(Date.parse(result))
  )
    throw new ApiFailure('unknown');
  return result;
}
function int(value: unknown, min = 0, max = 2147483647): number {
  if (typeof value !== 'number' || !Number.isInteger(value) || value < min || value > max)
    throw new ApiFailure('unknown');
  return value;
}
export function notificationProjection(value: unknown): UserNotification {
  const raw = object(value),
    resource = object(raw['resource']);
  return Object.freeze({
    id: id(raw['id']),
    kind: text(raw['kind']),
    title: text(raw['title']),
    message: text(raw['message']),
    occurredAtUtc: utc(raw['occurredAtUtc']),
    createdAtUtc: utc(raw['createdAtUtc']),
    readAtUtc: raw['readAtUtc'] === null ? null : utc(raw['readAtUtc']),
    resource: Object.freeze({
      kind: text(resource['kind']),
      id: id(resource['id']),
      customerId: nullableId(resource['customerId']),
      parentId: nullableId(resource['parentId']),
      relatedId: nullableId(resource['relatedId']),
    }),
  });
}
@Injectable({ providedIn: 'root' })
export class NotificationApi {
  private readonly http = inject(ApiTransport);
  async list(page = 1, filter: NotificationFilter = ''): Promise<NotificationPage> {
    if (
      !Number.isInteger(page) ||
      page < 1 ||
      page > 1000000 ||
      !['', 'read', 'unread'].includes(filter)
    )
      throw new ApiFailure('invalid');
    const raw = object(
      await this.http.get<unknown>(
        `/api/notifications?page=${page}&pageSize=20${filter ? '&readStatus=' + filter : ''}`,
      ),
    );
    if (!Array.isArray(raw['items'])) throw new ApiFailure('unknown');
    return Object.freeze({
      items: Object.freeze(raw['items'].map(notificationProjection)),
      totalCount: int(raw['totalCount']),
      page: int(raw['page'], 1, 1000000),
      pageSize: int(raw['pageSize'], 1, 100),
    });
  }
  async count(): Promise<number> {
    return int(
      object(await this.http.get<unknown>('/api/notifications/unread-count'))['unreadCount'],
    );
  }
  async detail(notificationId: string): Promise<UserNotification> {
    return notificationProjection(
      await this.http.get<unknown>('/api/notifications/' + crmId(notificationId)),
    );
  }
  async markRead(notificationId: string): Promise<NotificationReadReply> {
    const expected = crmId(notificationId);
    const reply = await this.http.put<unknown>(`/api/notifications/${expected}/read`, undefined);
    try {
      const raw = object(reply);
      if (id(raw['notificationId']) !== expected) throw new ApiFailure('unknown');
      return Object.freeze({ notificationId: expected, readAtUtc: utc(raw['readAtUtc']) });
    } catch {
      // Any malformed acknowledgement can follow a committed first read.
      throw new ApiFailure('unknown', [], true);
    }
  }
}
