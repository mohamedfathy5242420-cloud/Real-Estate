import { Injectable, inject } from '@angular/core';
import { ApiTransport } from './api-transport';
import { ApiFailure } from '../models/api-failure';
import { crmId } from './crm-api';
import { CrmPage } from '../models/crm';
import {
  ManagerDeal,
  DealSummary,
  CommissionSnapshot,
  CommissionCollection,
  CollectionSummary,
  ActivityReport,
  CurrencyTotal,
  SaleUnit,
  SaleFieldKind,
  SaleOperation,
  SaleReceipt,
  SaleFilters,
  MANAGER_DEAL_FIELDS,
  DEAL_SUMMARY_FIELDS,
  COMMISSION_SNAPSHOT_FIELDS,
  COMMISSION_COLLECTION_FIELDS,
  COLLECTION_SUMMARY_FIELDS,
  ACTIVITY_REPORT_FIELDS,
  CURRENCY_TOTAL_FIELDS,
  SALE_UNIT_FIELDS,
} from '../models/sales';

export function saleProjection<T>(
  raw: unknown,
  fields: Readonly<Record<string, SaleFieldKind>>,
): T {
  if (!raw || typeof raw !== 'object') throw new ApiFailure('unknown');
  const source = raw as Record<string, unknown>,
    result: Record<string, unknown> = {};
  for (const [key, kind] of Object.entries(fields)) {
    const value = source[key],
      type = kind.replace('?', '');
    if (value === null && kind.endsWith('?')) {
      result[key] = null;
      continue;
    }
    if (type === 'number') {
      if (!Number.isSafeInteger(value) || (value as number) < 0) throw new ApiFailure('unknown');
    } else if (type === 'boolean') {
      if (typeof value !== 'boolean') throw new ApiFailure('unknown');
    } else {
      if (typeof value !== 'string') throw new ApiFailure('unknown');
      if (type === 'id') crmId(value);
      if (
        ['version', 'count'].includes(type) &&
        (!/^(?:0|[1-9]\d{0,18})$/.test(value) ||
          (value.length === 19 && value > '9223372036854775807') ||
          (type === 'version' && value === '0'))
      )
        throw new ApiFailure('unknown');
      if (
        ['money', 'aggregate'].includes(type) &&
        !(
          type === 'aggregate'
            ? /^(?:0|[1-9]\d{0,35})(?:\.\d{1,2})?$/
            : /^(?:0|[1-9]\d{0,15})(?:\.\d{1,2})?$/
        ).test(value)
      )
        throw new ApiFailure('unknown');
      if (type === 'rate' && !/^\d{1,3}\.\d{4}$/.test(value)) throw new ApiFailure('unknown');
      if (/currencyCode$/i.test(key) && !/^[A-Z]{3}$/.test(value)) throw new ApiFailure('unknown');
    }
    result[key] = value;
  }
  return Object.freeze(result) as T;
}
function page<T>(raw: unknown, fields: Readonly<Record<string, SaleFieldKind>>): CrmPage<T> {
  const p = raw as CrmPage<unknown>;
  if (
    !p ||
    !Array.isArray(p.items) ||
    !Number.isSafeInteger(p.page) ||
    p.page < 1 ||
    !Number.isSafeInteger(p.pageSize) ||
    p.pageSize < 1 ||
    !Number.isSafeInteger(p.total) ||
    p.total < 0
  )
    throw new ApiFailure('unknown');
  return Object.freeze({
    page: p.page,
    pageSize: p.pageSize,
    total: p.total,
    items: Object.freeze(p.items.map((x) => saleProjection<T>(x, fields))),
  });
}
export function saleQuery(filters: SaleFilters, allowed: readonly string[], n?: number) {
  const query = new URLSearchParams();
  if (n !== undefined) {
    if (!Number.isInteger(n) || n < 1 || n > 1000000) throw new ApiFailure('invalid');
    query.set('page', String(n));
    query.set('pageSize', '20');
  }
  for (const key of allowed) {
    let value = filters[key]?.trim();
    if (!value) continue;
    if (key.endsWith('Id')) value = crmId(value);
    if (key === 'currencyCode') {
      value = value.toUpperCase();
      if (!/^[A-Z]{3}$/.test(value)) throw new ApiFailure('invalid');
    }
    if (/Date$|^saleLocal/.test(key) && !/^\d{4}-\d{2}-\d{2}$/.test(value))
      throw new ApiFailure('invalid');
    query.set(key, value);
  }
  return query.size ? '?' + query : '';
}
const dealRoot = (customer: string, id: string) =>
  `/api/crm/customers/${crmId(customer)}/deals/${crmId(id)}`;
const unitRoot = (customer: string, lead: string) =>
  `/api/crm/customers/${crmId(customer)}/leads/${crmId(lead)}/sale-units`;
@Injectable({ providedIn: 'root' })
export class SalesApi {
  private readonly http = inject(ApiTransport);
  list(filters: SaleFilters = {}, n = 1) {
    return this.http
      .get(
        '/api/crm/deals' +
          saleQuery(
            filters,
            [
              'customerId',
              'projectId',
              'unitId',
              'developerId',
              'agreementId',
              'currencyCode',
              'saleLocalFrom',
              'saleLocalThrough',
            ],
            n,
          ),
      )
      .then((r) => page<DealSummary>(r, DEAL_SUMMARY_FIELDS));
  }
  detail(customer: string, id: string) {
    return this.http
      .get(dealRoot(customer, id))
      .then((r) => saleProjection<ManagerDeal>(r, MANAGER_DEAL_FIELDS));
  }
  snapshot(customer: string, id: string) {
    return this.http
      .get(dealRoot(customer, id) + '/commission-snapshot')
      .then((r) => saleProjection<CommissionSnapshot>(r, COMMISSION_SNAPSHOT_FIELDS));
  }
  collections(customer: string, id: string, n = 1) {
    return this.http
      .get(dealRoot(customer, id) + '/collections' + saleQuery({}, [], n))
      .then((r) => page<CommissionCollection>(r, COMMISSION_COLLECTION_FIELDS));
  }
  summary(customer: string, id: string) {
    return this.http
      .get(dealRoot(customer, id) + '/collection-summary')
      .then((r) => saleProjection<CollectionSummary>(r, COLLECTION_SUMMARY_FIELDS));
  }
  activity(filters: SaleFilters = {}) {
    return this.http
      .get('/api/crm/reports/activity' + saleQuery(filters, ['fromUtcDate', 'throughUtcDate']))
      .then((r) => saleProjection<ActivityReport>(r, ACTIVITY_REPORT_FIELDS));
  }
  commissions(filters: SaleFilters = {}, n = 1) {
    return this.http
      .get(
        '/api/crm/reports/commissions' +
          saleQuery(
            filters,
            [
              'customerId',
              'projectId',
              'developerId',
              'currencyCode',
              'saleLocalFrom',
              'saleLocalThrough',
            ],
            n,
          ),
      )
      .then((r) => page<CurrencyTotal>(r, CURRENCY_TOTAL_FIELDS));
  }
  units(customer: string, lead: string, n = 1, q = '') {
    return this.http
      .get(unitRoot(customer, lead) + saleQuery({ q }, ['q'], n))
      .then((r) => page<SaleUnit>(r, SALE_UNIT_FIELDS));
  }
  unit(customer: string, lead: string, id: string) {
    return this.http
      .get(unitRoot(customer, lead) + '/' + crmId(id))
      .then((r) => saleProjection<SaleUnit>(r, SALE_UNIT_FIELDS));
  }
  async execute(op: SaleOperation): Promise<SaleReceipt> {
    const path =
      op.action === 'adopt'
        ? `/api/crm/customers/${crmId(op.customerId)}/deals`
        : dealRoot(op.customerId, op.dealId ?? '') + '/collections';
    const raw = await this.http.post(path, op.body, true, op.key);
    try {
      const r = saleProjection<{ id: string; version: string }>(raw, {
          id: 'id',
          version: 'version',
        }),
        replay = (raw as { isReplay: unknown }).isReplay;
      if (typeof replay !== 'boolean') throw new ApiFailure('unknown');
      return Object.freeze({ ...r, isReplay: replay });
    } catch {
      throw new ApiFailure('unknown', [], true);
    }
  }
}
