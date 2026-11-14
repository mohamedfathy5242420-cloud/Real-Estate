import { Injectable, inject } from '@angular/core';
import { ViewingApi } from '../api/viewing-api';
import { SessionContext } from './session-context.service';
import { ViewingAction, ViewingOperation, OwnViewing, StaffViewing } from '../models/viewing';
import { ApiFailure } from '../models/api-failure';
import { crmId } from '../api/crm-api';
/** Local date/time inputs explicitly use the device zone; reject normalized DST gaps/dates. */
export function viewingUtc(local: string): string {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?$/.test(local))
    throw new ApiFailure('invalid');
  const d = new Date(local);
  const parts = [d.getFullYear(), d.getMonth() + 1, d.getDate(), d.getHours(), d.getMinutes()];
  const expected = local.match(/\d+/g)!.slice(0, 5).map(Number);
  if (!Number.isFinite(d.getTime()) || parts.some((x, i) => x !== expected[i]))
    throw new ApiFailure('invalid');
  return d.toISOString();
}
@Injectable({ providedIn: 'root' })
export class ViewingService {
  private readonly api = inject(ViewingApi);
  private readonly session = inject(SessionContext);
  list(staff: boolean, page = 1, status = '') {
    return this.api.list(staff, page, status);
  }
  detail(staff: boolean, id: string, customerId?: string) {
    return this.api.detail(staff, id, customerId);
  }
  history(staff: boolean, id: string, customerId?: string, page = 1) {
    return this.api.history(staff, id, customerId, page);
  }
  command(
    action: ViewingAction,
    staff: boolean,
    current: OwnViewing | StaffViewing | null,
    fields: Record<string, string>,
  ): ViewingOperation {
    const body: Record<string, string | null> = {};
    if (action !== 'submit') {
      if (!current) throw new ApiFailure('invalid');
      body['version'] = current.version;
    }
    const fieldNames: Record<ViewingAction, readonly string[]> = {
      submit: ['unitId', 'proposedStartUtc'],
      'accept-alternative': [],
      cancel: staff ? ['cancellationKind', 'reason'] : ['reason'],
      'change-proposal': ['proposedStartUtc', 'reason'],
      confirm: [
        'confirmedStartUtc',
        'confirmedEndUtc',
        'coordinatedAtUtc',
        'coordinationSource',
        'coordinationReference',
      ],
      alternatives: [
        'proposedStartUtc',
        'proposedEndUtc',
        'coordinatedAtUtc',
        'coordinationSource',
        'coordinationReference',
        'reason',
      ],
      outcome: ['outcome', 'occurredAtUtc', 'notes'],
    };
    for (const key of fieldNames[action]) {
      // Confirmation must retain the current proposed start, including sub-millisecond wire precision.
      // A different start is the distinct alternative flow, never a silent confirmation edit.
      if (action === 'confirm' && key === 'confirmedStartUtc') {
        body[key] = current!.currentProposedStartUtc;
        continue;
      }
      const value = (fields[key] ?? '').trim();
      body[key] = key.endsWith('Utc')
        ? viewingUtc(value)
        : key === 'unitId'
          ? crmId(value)
          : value || null;
    }
    return Object.freeze({
      action,
      staff,
      ...(current ? { id: current.id } : {}),
      ...(staff && current && 'customerId' in current ? { customerId: current.customerId } : {}),
      key: crypto.randomUUID(),
      revision: this.session.revision(),
      body: Object.freeze(body),
    });
  }
  execute(op: ViewingOperation) {
    if (op.revision !== this.session.revision()) throw new ApiFailure('stale');
    return this.api.execute(op);
  }
}
