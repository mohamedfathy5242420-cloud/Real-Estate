import { Component, input, output } from '@angular/core';
@Component({
  selector: 'app-crm-pager',
  template: `<nav [attr.aria-label]="label()">
    <button type="button" [disabled]="busy() || page() <= 1" (click)="change.emit(-1)">
      Previous</button
    ><span>Page {{ page() }} · {{ total() }} Record</span
    ><button type="button" [disabled]="busy() || page() * 20 >= total()" (click)="change.emit(1)">
      Next
    </button>
  </nav>`,
  styles: [
    `
      nav {
        display: flex;
        gap: 0.75rem;
        align-items: center;
        flex-wrap: wrap;
        margin-block: 1rem;
      }
      button {
        padding: 0.7rem;
        font: inherit;
        min-block-size: 2.75rem;
        cursor: pointer;
      }
    `,
  ],
})
export class CrmPager {
  readonly label = input.required<string>();
  readonly busy = input(false);
  readonly page = input(1);
  readonly total = input(0);
  readonly change = output<number>();
}
