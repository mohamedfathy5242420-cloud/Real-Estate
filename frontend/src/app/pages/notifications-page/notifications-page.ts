import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeading } from '../../components/page-heading/page-heading';
import { CrmPager } from '../../components/crm-pager/crm-pager';
import { DisplayNumberPipe } from '../../pipes/display-format.pipe';
import { NotificationService } from '../../services/notification.service';
import { SessionContext } from '../../services/session-context.service';
import {
  NOTIFICATION_ROLES,
  NotificationFilter,
  NotificationPage,
  UserNotification,
} from '../../models/notification';
import { crmError } from '../../models/crm-ui';

@Component({
  selector: 'app-notifications-page',
  imports: [FormsModule, RouterLink, PageHeading, CrmPager, DisplayNumberPipe],
  templateUrl: './notifications-page.html',
  styleUrls: ['../account-page/account-page.css', '../crm-page.css', './notifications-page.css'],
})
export class NotificationsPage {
  protected readonly service = inject(NotificationService);
  protected readonly session = inject(SessionContext);
  protected readonly allowed = () =>
    NOTIFICATION_ROLES.some((r) => this.session.account()?.roles.includes(r));
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly rows = signal<NotificationPage | null>(null);
  protected readonly count = signal<number | null>(null);
  protected readonly selected = signal<UserNotification | null>(null);
  protected readonly page = signal(1);
  protected filter: NotificationFilter = '';
  private turn = 0;
  private revision = this.session.revision();
  constructor() {
    void this.reload();
    effect(() => {
      const r = this.session.revision();
      if (r !== this.revision || !this.allowed()) {
        this.revision = r;
        this.turn++;
        this.clear();
        this.busy.set(false);
        this.error.set('');
        this.message.set('');
      }
    });
  }
  private clear() {
    this.rows.set(null);
    this.count.set(null);
    this.selected.set(null);
  }
  private async readList() {
    const [rows, count] = await Promise.all([
      this.service.list(this.page(), this.filter),
      this.service.count(),
    ]);
    return { rows, count };
  }
  private async run(
    work: () => Promise<{
      rows: NotificationPage;
      count: number;
      selected?: UserNotification;
      message?: string;
    }>,
  ) {
    if (this.busy() || !this.allowed()) return;
    const t = ++this.turn,
      r = this.session.revision();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.clear();
    try {
      const result = await work();
      if (t !== this.turn || r !== this.session.revision() || !this.allowed()) return;
      this.rows.set(result.rows);
      this.count.set(result.count);
      this.selected.set(result.selected ?? null);
      this.message.set(result.message ?? '');
    } catch (e) {
      if (t === this.turn && r === this.session.revision()) {
        this.clear();
        this.error.set(crmError(e));
      }
    } finally {
      if (t === this.turn) this.busy.set(false);
    }
  }
  protected reload() {
    return this.run(() => this.readList());
  }
  protected async apply() {
    if (!this.busy()) {
      this.page.set(1);
      await this.reload();
    }
  }
  protected async move(delta: number) {
    if (!this.busy()) {
      this.page.update((p) => Math.max(1, Math.min(1000000, p + delta)));
      await this.reload();
    }
  }
  protected open(id: string) {
    return this.run(async () => ({
      ...(await this.readList()),
      selected: await this.service.detail(id),
    }));
  }
  protected markRead() {
    const row = this.selected();
    if (!row || row.readAtUtc) return Promise.resolve();
    return this.run(async () => {
      await this.service.markRead(row.id);
      return {
        ...(await this.readList()),
        selected: await this.service.detail(row.id),
        message: 'Read status recorded. A notification does not change request state.',
      };
    });
  }
  protected date(value: string) {
    return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(
      new Date(value),
    );
  }
}
