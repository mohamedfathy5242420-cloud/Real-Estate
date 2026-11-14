import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { StaffAccountsPage } from './staff-accounts-page';
import { StaffAccountsService } from '../../services/staff-accounts.service';
import { SessionContext } from '../../services/session-context.service';
import { ApiFailure } from '../../models/api-failure';
describe('FAC-11 staff form and identity isolation', () => {
  const create = vi.fn();
  beforeEach(() => {
    create.mockReset();
    TestBed.configureTestingModule({
      imports: [StaffAccountsPage],
      providers: [provideRouter([]), { provide: StaffAccountsService, useValue: { create } }],
    });
  });
  async function setup() {
    const scope = TestBed.inject(SessionContext);
    scope.accept(scope.revision(), {
      accountId: 'admin',
      email: 'admin@brokerhub.test',
      roles: ['SystemAdministrator'],
    });
    const fixture = TestBed.createComponent(StaffAccountsPage);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    for (const [id, value] of [
      ['staff-email', 'employee@brokerhub.test'],
      ['staff-phone', '+201000000001'],
      ['staff-password', 'Example!456'],
    ]) {
      const input = root.querySelector('#' + id) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    const submit = () =>
      root
        .querySelector('form')!
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    return { scope, fixture, root, submit };
  }
  it('does not duplicate a pending create and clears a late result after identity changes', async () => {
    let finish!: (value: unknown) => void;
    create.mockReturnValue(
      new Promise((resolve) => {
        finish = resolve;
      }),
    );
    const { scope, fixture, root, submit } = await setup();
    submit();
    submit();
    expect(create).toHaveBeenCalledTimes(1);
    scope.clear('expired');
    fixture.detectChanges();
    finish({ id: 'created', email: 'private-old@brokerhub.test', role: 'SalesEmployee' });
    await fixture.whenStable();
    expect(root.textContent).not.toContain('private-old@brokerhub.test');
    expect(root.querySelector('form')).toBeNull();
    expect(root.textContent).toContain('Session expired');
  });
  it('shows an honest unknown outcome without raw diagnostics or automatic retry', async () => {
    create.mockRejectedValue(new ApiFailure('network', [], true));
    const { fixture, root, submit } = await setup();
    submit();
    await fixture.whenStable();
    expect(root.textContent).toContain('Account creation result is uncertain');
    expect(create).toHaveBeenCalledTimes(1);
    expect((root.querySelector('#staff-password') as HTMLInputElement).value).toBe('');
  });
});
