import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ApiFailure } from './api-failure';
export function customerForm() {
  return new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(32)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email, Validators.maxLength(320)],
    }),
    preferredContactMethod: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(100)],
    }),
    requirements: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
    }),
  });
}
export function crmError(error: unknown): string {
  if (!(error instanceof ApiFailure))
    return 'Could not determine the result. Review the current state before submitting again.';
  if (error.outcomeUnknown)
    return 'The result is uncertain and may have been saved. Reload and review before submitting again.';
  return error.kind === 'conflict'
    ? 'The version or operation eligibility changed. Reload and review the data before trying again.'
    : error.kind === 'forbidden'
      ? 'You no longer have permission to view this data.'
      : error.kind === 'unauthorized'
        ? 'Your session expired. Sign in again.'
        : error.kind === 'not-found'
          ? 'The resource no longer exists or is unavailable to your account.'
          : error.kind === 'invalid'
            ? 'Review the form and required fields.'
            : error.kind === 'stale'
              ? 'The session or page changed. Previous request data was not displayed.'
              : 'Could not reach the server. Try later without repeating any unresolved operation.';
}
