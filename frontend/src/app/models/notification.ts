export const NOTIFICATION_ROLES = ['Customer', 'SalesEmployee', 'SalesManager'] as const;
export interface NotificationResource {
  readonly kind: string;
  readonly id: string;
  readonly customerId: string | null;
  readonly parentId: string | null;
  readonly relatedId: string | null;
}
export interface UserNotification {
  readonly id: string;
  readonly kind: string;
  readonly title: string;
  readonly message: string;
  readonly occurredAtUtc: string;
  readonly createdAtUtc: string;
  readonly readAtUtc: string | null;
  readonly resource: NotificationResource;
}
export interface NotificationPage {
  readonly items: readonly UserNotification[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}
export type NotificationFilter = '' | 'read' | 'unread';
export interface NotificationReadReply {
  readonly notificationId: string;
  readonly readAtUtc: string;
}
