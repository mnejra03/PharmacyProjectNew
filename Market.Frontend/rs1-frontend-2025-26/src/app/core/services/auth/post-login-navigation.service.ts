import { Injectable, inject } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';
import { AuthFacadeService } from './auth-facade.service';
import { DialogButton } from '../../../modules/shared/models/dialog-config.model';
import { DialogHelperService } from '../../../modules/shared/services/dialog-helper.service';

@Injectable({ providedIn: 'root' })
export class PostLoginNavigationService {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthFacadeService);
  private readonly dialogs = inject(DialogHelperService);
  private panelPrefix: '/admin' | '/client' | '' = '';
  private armed = false;
  private replacingLoginWithBoundary = false;
  private backDialogOpen = false;
  private restoringPopState = false;
  private lastNavigationTrigger: NavigationStart['navigationTrigger'] = 'imperative';

  private readonly onPopState = (event: PopStateEvent): void => {
    if (!this.armed) return;

    const currentUrl = this.cleanUrl(this.router.url);
    const destinationUrl = this.cleanUrl(`${window.location.pathname}${window.location.search}${window.location.hash}`);

    // history.forward() below restores the page whose Back action was blocked.
    // Let this synthetic event stop here so Angular does not start another route change.
    if (this.restoringPopState && destinationUrl === currentUrl) {
      event.stopImmediatePropagation();
      this.restoringPopState = false;
      return;
    }

    const leavingPanel = this.isInsidePanel(currentUrl) && !this.isInsidePanel(destinationUrl);
    if (!this.backDialogOpen && !leavingPanel) return;

    // The first Back opens the confirmation. Any further Back while it is open
    // dismisses it and is reversed, leaving the current panel page in place.
    event.preventDefault();
    event.stopImmediatePropagation();
    this.restoreCurrentHistoryEntry();

    if (this.backDialogOpen) {
      this.dialogs.dismissLogoutFromBrowserBack();
      return;
    }

    this.backDialogOpen = true;
    this.dialogs.confirmLogoutFromBrowserBack().subscribe(result => {
      this.backDialogOpen = false;
      if (result?.button !== DialogButton.YES) return;

      this.disarm();
      this.auth.logout().subscribe(() => {
        void this.router.navigateByUrl('/auth/login', { replaceUrl: true });
      });
    });
  };

  constructor() {
    if (typeof window !== 'undefined') window.addEventListener('popstate', this.onPopState, true);

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.lastNavigationTrigger = event.navigationTrigger;
        return;
      }
      if (!(event instanceof NavigationEnd) || !this.armed) return;

      const currentUrl = this.cleanUrl(event.urlAfterRedirects);
      if (this.replacingLoginWithBoundary) {
        // The temporary public home entry becomes the Back boundary between
        // this login session and any pages left by a previous account/role.
        if (this.isInsidePanel(currentUrl)) this.replacingLoginWithBoundary = false;
        return;
      }

      // Keep the session boundary while navigating among pages in this role.
      // Disarm only after a completed, deliberate navigation out of the panel.
      if (!this.isInsidePanel(currentUrl) && this.lastNavigationTrigger === 'imperative') {
        this.disarm();
      }
    });
  }

  /** Start a role-specific route history with a clean boundary behind it. */
  navigateAfterLogin(target: string): Promise<boolean> {
    this.panelPrefix = target.startsWith('/admin') ? '/admin' : '/client';
    this.armed = true;
    this.replacingLoginWithBoundary = true;

    // Replace the login entry with public home, then push the role landing
    // page. Browser Back can now traverse this session's own routes only;
    // it cannot expose another role's stale route history.
    return this.router.navigateByUrl('/', { replaceUrl: true }).then(atBoundary => {
      if (!atBoundary) {
        this.disarm();
        return false;
      }
      return this.router.navigateByUrl(target);
    }).then(navigated => {
      if (!navigated) this.disarm();
      return navigated;
    });
  }

  shouldConfirmBackOut(nextUrl: string): boolean {
    return this.armed
      && this.lastNavigationTrigger === 'popstate'
      && this.isInsidePanel(this.router.url)
      && !this.isInsidePanel(nextUrl);
  }

  disarm(): void {
    this.armed = false;
    this.panelPrefix = '';
    this.replacingLoginWithBoundary = false;
    this.backDialogOpen = false;
    this.restoringPopState = false;
  }

  private restoreCurrentHistoryEntry(): void {
    this.restoringPopState = true;
    window.history.forward();
  }

  private isInsidePanel(url: string): boolean {
    const path = this.cleanUrl(url);
    return !!this.panelPrefix && (path === this.panelPrefix || path.startsWith(`${this.panelPrefix}/`));
  }

  private cleanUrl(url: string): string {
    const path = url.split(/[?#]/, 1)[0].replace(/\/+$/, '');
    return path || '/';
  }
}
