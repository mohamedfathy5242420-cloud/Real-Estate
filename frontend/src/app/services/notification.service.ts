import { Injectable, inject } from '@angular/core';
import { NotificationApi } from '../api/notification-api';
import { SessionContext } from './session-context.service';
import { ApiFailure } from '../models/api-failure';
import { NOTIFICATION_ROLES, NotificationFilter, UserNotification } from '../models/notification';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(NotificationApi);
  private readonly session = inject(SessionContext);
  private async current<T>(read: () => Promise<T>): Promise<T> {
    const revision = this.session.revision();
    if (
      this.session.status() !== 'authenticated' ||
      !NOTIFICATION_ROLES.some((r) => this.session.account()?.roles.includes(r))
    )
      throw new ApiFailure('forbidden');
    const result = await read();
    if (revision !== this.session.revision()) throw new ApiFailure('stale');
    return result;
  }
  list(page: number, filter: NotificationFilter) {
    return this.current(() => this.api.list(page, filter));
  }
  count() {
    return this.current(() => this.api.count());
  }
  detail(id: string) {
    return this.current(() => this.api.detail(id));
  }
  markRead(id: string) {
    return this.current(() => this.api.markRead(id));
  }
  destination(row: UserNotification): { path: string; label: string } | null {
    const roles = this.session.account()?.roles ?? [];
    const owner = roles.includes('Customer'),
      manager = roles.includes('SalesManager'),
      employee = roles.includes('SalesEmployee');
    if (!owner && !manager && !employee) return null;
    if (row.kind.startsWith('Viewing.'))
      return {
        path: manager || employee ? '/employee/viewings' : '/my/viewings',
        label: 'Manage viewings',
      };
    if (row.kind.startsWith('Booking.'))
      return {
        path: manager ? '/admin/bookings' : employee ? '/employee/bookings' : '/my/bookings',
        label: 'Manage bookings',
      };
    if (row.kind.startsWith('Crm.'))
      return {
        path: manager ? '/admin/clients' : employee ? '/employee/clients' : '/my/profile',
        label: 'Customer profile and follow-up',
      };
    if (
      manager &&
      ['Deal.Adopted', 'Commission.CollectionRecorded', 'Catalog.UnitUnpublished'].includes(
        row.kind,
      )
    )
      return { path: '/admin/deals', label: 'Deals and commissions' };
    return null;
  }
}
