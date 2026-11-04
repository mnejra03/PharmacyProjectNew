// src/app/core/services/auth/auth-facade.service.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, map } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import {
  LoginCommand,
  LoginCommandDto,
  LogoutCommand,
  RefreshTokenCommand,
  RefreshTokenCommandDto,
  RegisterCommand,
} from '../../../api-services/auth/auth-api.model';

import { AuthStorageService } from './auth-storage.service';
import { CurrentUserDto } from './current-user.dto';
import { JwtPayloadDto } from './jwt-payload.dto';

/**
 * Main authentication facade service.
 * - communicates with AuthApiService over HTTP
 * - communicates with AuthStorageService and localStorage
 * - decodes the JWT and exposes CurrentUser as a signal
 *
 * Used by:
 * - the interceptor (getAccessToken, refresh)
 * - route guards (isAuthenticated, isAdmin)
 * - components (login, logout, navigation bar)
 */
@Injectable({ providedIn: 'root' })
export class AuthFacadeService {
  private api = inject(AuthApiService);
  private storage = inject(AuthStorageService);
  private router = inject(Router);

  // === Reactive state: current user ===

  private _currentUser = signal<CurrentUserDto | null>(null);

  /** read-only signal for the UI, accessed as auth.currentUser() */
  currentUser = this._currentUser.asReadonly();

  /** computed signals for the current user */
  isAuthenticated = computed(() => !!this._currentUser());
  isAdmin = computed(() => this._currentUser()?.isAdmin ?? false);
  isPharmacist = computed(() => this._currentUser()?.isPharmacist ?? false);
  isCustomer = computed(() => this._currentUser()?.isCustomer ?? false);

  constructor() {
    // initialize state from the existing access token
    this.initializeFromToken();
  }

  // =========================================================
  // PUBLIC API
  // =========================================================

  /**
   * Log in with an email address and password.
   * Store the tokens, decode the JWT, and populate the current user state.
   */
  login(payload: LoginCommand): Observable<void> {
    return this.api.login(payload).pipe(
      tap((response: LoginCommandDto) => {
        this.storage.saveLogin(response);           // access token, refresh token, and expiration times
        this.decodeAndSetUser(response.accessToken); // populate _currentUser
      }),
      map(() => void 0)
    );
  }
  register(payload: RegisterCommand): Observable<void> {
    return this.api.register(payload).pipe(tap(response => { this.storage.saveLogin(response); this.decodeAndSetUser(response.accessToken); }), map(() => void 0));
  }

  /**
   * Log out the user:
   * - clear local state and tokens
   * - attempt to invalidate the refresh token on the server and ignore errors
   */
  logout(): Observable<void> {
    const refreshToken = this.storage.getRefreshToken();

    // 1) clear local state first for an optimistic logout
    this.clearUserState();

    // 2) without a refresh token, no API request is needed
    if (!refreshToken) {
      return of(void 0);
    }

    const payload: LogoutCommand = { refreshToken };

    // 3) attempt server-side logout and ignore failures
    return this.api.logout(payload).pipe(catchError(() => of(void 0)));
  }

  /**
   * Refresh the access token using the refresh token.
   * Called by the interceptor after a 401 response.
   */
  refresh(payload: RefreshTokenCommand): Observable<RefreshTokenCommandDto> {
    return this.api.refresh(payload).pipe(
      tap((response: RefreshTokenCommandDto) => {
        this.storage.saveRefresh(response);           // store the new tokens
        this.decodeAndSetUser(response.accessToken);  // update the current user
      })
    );
  }

  /**
   * Utility for route guards and interceptors; clears auth state and navigates to /login.
   */
  redirectToLogin(): void {
    this.clearUserState();
    this.router.navigate(['/login']);
  }

  // =========================================================
  // GETTERS FOR THE INTERCEPTOR
  // =========================================================

  /**
   * Access token for the Authorization header.
   */
  getAccessToken(): string | null {
    return this.storage.getAccessToken();
  }

  /**
   * Refresh token for the refresh request.
   */
  getRefreshToken(): string | null {
    return this.storage.getRefreshToken();
  }

  // =========================================================
  // PRIVATE HELPERS
  // =========================================================

  /**
   * On application startup, restore state from the existing token.
   */
  private initializeFromToken(): void {
    const token = this.storage.getAccessToken();
    if (token) {
      this.decodeAndSetUser(token);
    }
  }

  /**
   * Decode the JWT and set the current user state.
   */
  private decodeAndSetUser(token: string): void {
    try {
      const payload = jwtDecode<JwtPayloadDto>(token);

      const user: CurrentUserDto = {
        userId: Number(payload.sub),
        email: payload.email,
        isAdmin: payload.is_admin === 'true',
        isPharmacist: payload.is_pharmacist === 'true',
        isCustomer: payload.is_customer === 'true',
        tokenVersion: Number(payload.ver),
      };

      this._currentUser.set(user);
    } catch (error) {
      console.error('Failed to decode JWT token:', error);
      this._currentUser.set(null);
    }
  }

  /**
   * Clear user state and all tokens from storage.
   */
  private clearUserState(): void {
    this._currentUser.set(null);
    this.storage.clear();
  }
}
