import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { CurrentUserService } from '../services/auth/current-user.service';
import { ToasterService } from '../services/toaster.service';

export const guestOnlyGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const toaster = inject(ToasterService);

  if (!currentUser.isAuthenticated()) return true;

  toaster.info('Već ste prijavljeni. Otvaram vašu početnu stranicu.');
  return router.parseUrl(currentUser.getDefaultRoute());
};
