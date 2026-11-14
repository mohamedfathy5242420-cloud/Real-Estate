import { Component, input } from '@angular/core';
import { DisplayNumberPipe } from '../../pipes/display-format.pipe';
import { formatProjectTime } from '../../models/project-time';
import { bookingLabel } from '../../models/booking';
export interface BookingDisplayField {
  readonly key: string;
  readonly label: string;
  readonly value: string | null;
  readonly type: string;
}
@Component({
  selector: 'app-booking-fields',
  imports: [DisplayNumberPipe],
  template: `<dl>
    @for (field of fields(); track field.key) {
      <dt>{{ field.label }}</dt>
      <dd>
        @if (field.value === null) {
          Not recorded
        } @else if (field.type === 'time') {
          {{ date(field.value) }}
        } @else if (field.type === 'money') {
          {{ field.value | displayNumber }}
        } @else if (field.type === 'label') {
          {{ label(field.value) }}
        } @else {
          <bdi>{{ field.value }}</bdi>
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
export class BookingFields {
  readonly fields = input.required<readonly BookingDisplayField[]>();
  readonly zone = input('UTC');
  protected readonly label = bookingLabel;
  protected date(value: string) {
    return formatProjectTime(value, this.zone());
  }
}
