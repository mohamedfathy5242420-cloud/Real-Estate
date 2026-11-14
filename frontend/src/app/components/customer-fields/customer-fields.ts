import { Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { customerForm } from '../../models/crm-ui';
@Component({
  selector: 'app-customer-fields',
  imports: [ReactiveFormsModule],
  template: `<div [formGroup]="form()" class="fields">
    <label
      >Full name *<input
        id="customer-name"
        formControlName="fullName"
        maxlength="200"
        autocomplete="name"
    /></label>
    <label
      >Phone number *<input
        id="customer-phone"
        formControlName="phone"
        maxlength="32"
        type="tel"
        dir="ltr"
        autocomplete="tel"
    /></label>
    <label
      >Email (optional)<input
        id="customer-email"
        formControlName="email"
        maxlength="320"
        type="email"
        dir="ltr"
        autocomplete="email"
    /></label>
    <label
      >Preferred contact method<input
        id="customer-contact-method"
        formControlName="preferredContactMethod"
        maxlength="100"
    /></label>
    <label
      >Requirements<input
        id="customer-requirements"
        formControlName="requirements"
        maxlength="2000"
    /></label>
    @if (form().touched && form().invalid) {
      <p role="alert">Enter a name and phone number, and check the email and text lengths.</p>
    }
  </div>`,
  styles: [
    `
      :host {
        display: block;
      }
      .fields {
        display: grid;
        gap: 1rem;
      }
      label {
        display: grid;
        gap: 0.5rem;
      }
      input {
        box-sizing: border-box;
        inline-size: 100%;
        min-inline-size: 0;
        padding: 0.8rem;
        font: inherit;
        border: 1px solid #7b8c83;
        border-radius: 0.5rem;
      }
      input:focus-visible {
        outline: 3px solid #947329;
        outline-offset: 3px;
      }
    `,
  ],
})
export class CustomerFields {
  readonly form = input.required<ReturnType<typeof customerForm>>();
}
