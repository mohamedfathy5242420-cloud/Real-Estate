import { Component, input } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CatalogEntity, CatalogField } from '../../models/catalog-admin';
@Component({
  selector: 'app-catalog-fields',
  imports: [ReactiveFormsModule],
  template: ` <div [formGroup]="form()" class="fields">
    @for (field of fields(); track field.key) {
      <div class="field">
        <label [for]="'field-' + field.key"
          >{{ field.label }}{{ field.required ? ' *' : '' }}</label
        >
        @if (field.type === 'select') {
          <select [id]="'field-' + field.key" [formControlName]="field.key">
            <option value="">Select…</option>
            @if (field.choices === 'scope') {
              <option value="AllDeveloperProjects">
                All developer projects, including new projects
              </option>
              <option value="SelectedProjects">Selected projects</option>
            }
            @for (
              item of field.choices === 'developers'
                ? developers()
                : field.choices === 'projects'
                  ? projects()
                  : [];
              track item.id
            ) {
              <option [value]="item.id">{{ label(item) }}</option>
            }
          </select>
        } @else if (field.type === 'textarea') {
          <textarea
            [id]="'field-' + field.key"
            [formControlName]="field.key"
            [attr.maxlength]="field.max ?? null"
            rows="4"
          ></textarea>
        } @else {
          <input
            [id]="'field-' + field.key"
            [formControlName]="field.key"
            [type]="field.type === 'date' ? 'date' : 'text'"
            [attr.inputmode]="
              field.type === 'decimal' ? 'decimal' : field.type === 'integer' ? 'numeric' : null
            "
            [attr.maxlength]="field.max ?? null"
            [attr.dir]="
              field.type === 'decimal' ||
              field.type === 'integer' ||
              field.type === 'date' ||
              [
                'code',
                'currency',
                'timeZoneId',
                'externalContactEmail',
                'externalContactPhone',
              ].includes(field.key)
                ? 'ltr'
                : null
            "
          />
        }
        @if (form().controls[field.key].touched && form().controls[field.key].invalid) {
          <p class="field-error" role="alert">Review {{ field.label }}.</p>
        }
      </div>
    }
  </div>`,
  styles: [
    `
      :host {
        display: block;
      }
      .fields {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
        gap: 1rem;
      }
      .field {
        display: grid;
        gap: 0.5rem;
        min-inline-size: 0;
      }
      label {
        font-weight: 600;
      }
      input,
      select,
      textarea {
        box-sizing: border-box;
        inline-size: 100%;
        min-inline-size: 0;
        font: inherit;
        padding: 0.75rem;
        border: 1px solid #7b8c83;
        border-radius: 0.5rem;
        background: white;
        color: #233831;
      }
      input:disabled,
      select:disabled,
      textarea:disabled {
        background: #eef0ed;
        color: #4c5853;
      }
      .field-error {
        color: #8a2424;
        margin: 0;
      }
      :is(input, select, textarea):focus-visible {
        outline: 3px solid #947329;
        outline-offset: 3px;
      }
    `,
  ],
})
export class CatalogFields {
  readonly form = input.required<FormGroup<Record<string, FormControl<string>>>>();
  readonly fields = input.required<readonly CatalogField[]>();
  readonly developers = input<readonly CatalogEntity[]>([]);
  readonly projects = input<readonly CatalogEntity[]>([]);
  protected label(item: CatalogEntity): string {
    return item.kind === 'developers' || item.kind === 'projects'
      ? item.code + ' — ' + item.name + (item.isActive ? '' : ' (Inactive)')
      : '';
  }
}
