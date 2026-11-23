import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';
import { NotificationsApiService } from '../../../api-services/notifications/notifications-api.service';
import { StoreApiService } from '../../../api-services/store/store-api.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-client-layout',
  standalone: false,
  template: `<nav class="client-nav">
    <a mat-button routerLink="/" class="home-link"><mat-icon>home</mat-icon><span>Početna apoteke</span></a>
    <a mat-button routerLink="/catalog">Proizvodi</a>
    <a mat-button routerLink="/client/cart" class="cart-link">Korpa<ng-container *ngIf="cartCount$ | async as count"><span *ngIf="count > 0" class="cart-badge" [attr.aria-label]="count + ' artikala u korpi'">{{count}}</span></ng-container></a>
    <a mat-button routerLink="/client/wishlist">Favoriti</a>
    <a mat-button routerLink="/client/orders">Narudžbe</a>
    <a mat-button routerLink="/client/profile">Profil</a>
    <a mat-button *ngIf="!auth.isPharmacist() && !auth.isAdmin()" routerLink="/client/recipes">Moji recepti</a>
    <a mat-button *ngIf="auth.isPharmacist() || auth.isAdmin()" routerLink="/client/pharmacist/recipes">Recepti za pregled</a>
    <a mat-button *ngIf="auth.isPharmacist()" routerLink="/client/supply-orders">Nabavka i zalihe</a>
    <a mat-button routerLink="/client/chat">Razgovor</a>
    <a mat-button routerLink="/client/notifications" class="notification-link">Obavijesti<span *ngIf="unreadCount$ | async as unread"><span *ngIf="unread > 0" class="notification-dot" [attr.aria-label]="unread + ' nepročitanih obavijesti'"></span></span></a>
    <a mat-button routerLink="/auth/logout">Odjava</a>
  </nav><router-outlet></router-outlet>`,
  styles: [`
    .client-nav{display:flex;justify-content:center;align-items:center;flex-wrap:wrap;gap:.35rem;padding:.7rem 1rem;background:#174d2b}
    .client-nav a{color:#fff;border-radius:8px;font-weight:600}
    .client-nav a:hover,.client-nav a.active{background:#287342}
    .client-nav .home-link{display:inline-flex;align-items:center;gap:.35rem;min-height:38px;padding:0 .7rem;border:1px solid rgba(255,255,255,.38);background:rgba(255,255,255,.09);color:#fff;white-space:nowrap}
    .client-nav .home-link mat-icon{width:19px;height:19px;margin:0;font-size:19px;line-height:19px}
    .client-nav .home-link:hover{border-color:rgba(255,255,255,.7);background:rgba(255,255,255,.18)}
    .client-nav .cart-link{display:inline-flex;align-items:center;gap:.4rem}
    .cart-badge{display:inline-flex;align-items:center;justify-content:center;min-width:20px;height:20px;padding:0 5px;border-radius:999px;background:#d62828;color:#fff;font-size:.72rem;font-weight:800;line-height:1;box-shadow:0 1px 3px #0003}
    .client-nav .notification-link{display:inline-flex;align-items:center;gap:.45rem}
    .notification-dot{display:inline-block;width:9px;height:9px;border:1px solid #fff;border-radius:50%;background:#ef2929;box-shadow:0 0 0 2px #ef292933}
    @media(max-width:600px){.client-nav{gap:.2rem;padding:.55rem .4rem}.client-nav a{font-size:.82rem;padding:0 .45rem}}
  `]
})
export class ClientLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthFacadeService);
  private realtime = inject(ChatRealtimeService);
  private notifications = inject(NotificationsApiService);
  private store = inject(StoreApiService);
  readonly unreadCount$ = this.notifications.unreadCount$;
  readonly cartCount$ = this.store.cartCount$;
  private notificationSub?: Subscription;

  ngOnInit(): void {
    this.realtime.start();
    this.store.getCart().subscribe({ error: () => this.store.setCartCount(0) });
    this.loadNotificationCount();
    this.notificationSub = this.realtime.notifications.subscribe(() => this.loadNotificationCount());
  }
  ngOnDestroy(): void { this.notificationSub?.unsubscribe(); void this.realtime.stop(); }
  private loadNotificationCount(): void {
    this.notifications.getAll().subscribe({ error: () => this.notifications.setUnreadCount(0) });
  }
}
