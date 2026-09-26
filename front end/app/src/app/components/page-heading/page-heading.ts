import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-heading',
  templateUrl: './page-heading.html',
  styleUrl: './page-heading.css',
})
export class PageHeading {
  readonly eyebrow = input('');
  readonly title = input.required<string>();
  readonly description = input.required<string>();
}
