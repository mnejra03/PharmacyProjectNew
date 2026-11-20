import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { map } from 'rxjs';
import { CurrentUserService } from '../services/auth/current-user.service';
import { DialogButton } from '../../modules/shared/models/dialog-config.model';
import { DialogHelperService } from '../../modules/shared/services/dialog-helper.service';

export const confirmLogoutGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const dialogs = inject(DialogHelperService);

  if (!currentUser.isAuthenticated()) return true;
  return dialogs.confirmLogout().pipe(map(result => result?.button === DialogButton.YES));
};
