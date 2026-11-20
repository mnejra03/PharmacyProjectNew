import { Injectable, inject } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class PostLoginNavigationService {
  private readonly router = inject(Router);
  private panelPrefix: '/admin' | '/client' | '' = '';
  private armed = false;
  private replacingLoginWithBoundary = false;
  private lastNavigationTrigger: NavigationStart['navigationTrigger'] = 'imperative';

  constructor() {
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
