import { Component, input } from '@angular/core';
import { DisplayNumberPipe } from '../../pipes/display-format.pipe';
import { formatProjectTime } from '../../models/project-time';

const labels: Record<string, string> = {
  id: 'Record reference',
  customerId: 'Customer reference',
  unitId: 'Unit reference',
  projectId: 'Project reference',
  leadId: 'Original lead',
  leadVersion: 'Saved lead version',
  developerId: 'Developer reference',
  agreementId: 'Original marketing agreement',
  agreementVersion: 'Saved agreement version',
  agreementNumber: 'Agreement number',
  agreementScope: 'Agreement scope',
  agreementStartsOn: 'Agreement start',
  agreementEndsOn: 'Agreement end',
  protectionPeriodDays: 'Protection period in days',
  protectionEndsOn: 'Last protection day',
  leadOpenedOnLocalDate: 'Local lead opening day',
  openingTimeZoneId: 'Saved lead opening time',
  ruleId: 'Saved commission rule',
  ruleVersion: 'Commission rule version',
  ruleProjectId: 'Project rule override',
  ruleType: 'Commission type',
  percentage: 'Saved percentage',
  fixedAmount: 'Saved fixed amount',
  currencyCode: 'Currency',
  saleLocalDate: 'Saved local sale day',
  finalUnitPrice: 'Final unit sale price',
  amount: 'Saved amount',
  commissionAmount: 'Saved commission',
  saleOccurredAtUtc: 'Actual external sale time',
  source: 'External source',
  reference: 'External reference',
  selectionReason: 'Reason for selecting eligible origin',
  adoptedAtUtc: 'Deal approval time',
  adoptedByAccountId: 'Deal approver account',
  version: 'Record version',
  bookingId: 'Explicitly linked booking',
  dealId: 'Deal reference',
  requestedVersion: 'Deal version when collection was recorded',
  entitledAmount: 'Saved commission due',
  collectedAmount: 'Actual recorded collection',
  remainingAmount: 'Uncollected balance',
  collectionCount: 'Number of collections',
  collectedAtUtc: 'Actual external collection time',
  recordedAtUtc: 'Record creation time',
  recorderAccountId: 'Recorder account',
  notes: 'Internal notes',
  lastRecordedAtUtc: 'Latest collection record',
  observedAtUtc: 'Report read time',
  scope: 'Report scope',
  customerCount: 'Customers',
  unassignedCustomerCount: 'Unassigned customers',
  leadCount: 'Leads',
  openLeadCount: 'Open leads',
  unitCount: 'Units',
  viewingCount: 'Viewings',
  awaitingViewingCount: 'Currently pending viewings',
  confirmedViewingCount: 'Confirmed viewings',
  bookingCount: 'Booking requests',
  pendingBookingCount: 'Currently pending booking requests',
  confirmedBookingCount: 'Active confirmed bookings',
  dealCount: 'Approved deals',
};
const values: Record<string, string> = {
  Percentage: 'Percentage of final sale price',
  FixedAmount: 'Fixed amount',
  AllDeveloperProjects: 'All developer projects',
  SelectedProjects: 'Selected projects',
  AllCustomersAndCatalog: 'All customers and catalog under manager permissions',
  AssignedCustomers: 'Customers currently assigned to the employee',
};
@Component({
  selector: 'app-sales-fields',
  imports: [DisplayNumberPipe],
  template: `<dl>
    @for (field of fields(); track field.key) {
      <dt>{{ field.label }}</dt>
      <dd>
        @if (field.value === null) {
          Not recorded
        } @else if (field.key.endsWith('Utc')) {
          {{ date(field.value) }}
        } @else if (field.key === 'percentage') {
          <bdi>{{ rate(field.value) }}</bdi> %
        } @else if (field.numeric) {
          {{ field.value | displayNumber }}
        } @else {
          <bdi>{{ value(field.value) }}</bdi>
        }
      </dd>
    }
  </dl>`,
  styles: [
    `
      :host {
        display: block;
        min-inline-size: 0;
      }
      dl {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1.5fr);
        gap: 0.7rem;
      }
      dd {
        margin: 0;
        overflow-wrap: anywhere;
        white-space: pre-wrap;
      }
      dt {
        font-weight: 600;
      }
      @media (max-width: 480px) {
        dl {
          display: block;
        }
        dd {
          margin-block: 0.4rem 1rem;
        }
      }
    `,
  ],
})
export class SalesFields {
  readonly record = input.required<object>();
  readonly zone = input('UTC');
  protected fields() {
    return Object.entries(this.record()).map(([key, value]) => ({
      key,
      label: labels[key] ?? 'Saved reference',
      value: value === null ? null : String(value),
      numeric: /count$|amount$|price$|Days$/i.test(key) && key !== 'percentage',
    }));
  }
  protected date(value: string) {
    return formatProjectTime(value, this.zone());
  }
  protected value(value: string) {
    return values[value] ?? value;
  }
  protected rate(value: string) {
    return value.replace(/\d/g, (d) => '0123456789'[parseInt(d, 10)]).replace('.', '.');
  }
}
