export interface Account {
  readonly accountId: string;
  readonly email: string;
  readonly roles: readonly string[];
}
export { ApiFailure as AccountFailure } from './api-failure';
export type { ApiFailureKind as AccountFailureKind, ApiField as AccountField } from './api-failure';
