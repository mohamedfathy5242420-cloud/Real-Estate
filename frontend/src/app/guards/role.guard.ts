import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account.service';
import { canVisit, safeReturnUrl } from '../models/role-navigation';

/** UX boundary only; every real API request is independently authorized by the server. */
export const roleGuard: CanActivateFn = async (route, state) => {
  const accounts = inject(AccountService);
  const router = inject(Router);
  try {
    const account = await accounts.ensure(true);
    if (!account)
      return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl: safeReturnUrl(state.url) },
      });
    const roles = route.data['roles'] as readonly string[] | undefined;
    return (
      !roles ||
      canVisit(account, roles) ||
      router.createUrlTree(['/access'], { queryParams: { reason: 'forbidden' } })
    );
  } catch {
    return router.createUrlTree(['/access'], {
      queryParams: {
        reason: accounts.status() === 'forbidden' ? 'forbidden' : 'unavailable',
        returnUrl: safeReturnUrl(state.url),
      },
    });
  }
};
