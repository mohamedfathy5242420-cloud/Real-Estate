import { StaffRole } from './role-navigation';
export interface StaffAccountInput {
  email: string;
  password: string;
  phoneNumber: string;
  role: StaffRole;
}
export interface StaffAccountCreated {
  readonly id: string;
  readonly email: string;
  readonly role: StaffRole;
}
