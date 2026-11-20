import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { map, of, switchMap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthFacadeService } from '../services/auth/auth-facade.service';
import { CurrentUserService } from '../services/auth/current-user.service';
import { PostLoginNavigationService } from '../services/auth/post-login-navigation.service';
import { DialogButton } from '../../modules/shared/models/dialog-config.model';
import { DialogHelperService } from '../../modules/shared/services/dialog-helper.service';

export const confirmPanelBackGuard: CanDeactivateFn<unknown> = (_component, _currentRoute, _currentState, nextState) => {
  const router = inject(Router);
  const currentUser = inject(CurrentUserService);
  const auth = inject(AuthFacadeService);
  const postLoginNavigation = inject(PostLoginNavigationService);
  const dialogs = inject(DialogHelperService);
  if (!currentUser.isAuthenticated() || !postLoginNavigation.shouldConfirmBackOut(nextState.url)) return true;

  return dialogs.confirmLogout().pipe(
    switchMap(result => {
      if (result?.button !== DialogButton.YES) return of(false);
      return auth.logout().pipe(map(() => {
        postLoginNavigation.disarm();
        void router.navigateByUrl('/auth/login', { replaceUrl: true });
        return false;
      }));
    })
  );
};
