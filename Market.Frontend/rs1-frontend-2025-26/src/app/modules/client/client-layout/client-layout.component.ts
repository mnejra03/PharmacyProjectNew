import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';
import { NotificationsApiService } from '../../../api-services/notifications/notifications-api.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-client-layout',
  standalone: false,
  template: `<nav class="client-nav">
    <a mat-button routerLink="/catalog">Proizvodi</a>
    <a mat-button routerLink="/client/cart">Korpa</a>
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
    .client-nav .notification-link{display:inline-flex;align-items:center;gap:.45rem}
    .notification-dot{display:inline-block;width:9px;height:9px;border:1px solid #fff;border-radius:50%;background:#ef2929;box-shadow:0 0 0 2px #ef292933}
    @media(max-width:600px){.client-nav{gap:.2rem;padding:.55rem .4rem}.client-nav a{font-size:.82rem;padding:0 .45rem}}
  `]
})
export class ClientLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthFacadeService);
  private realtime = inject(ChatRealtimeService);
  private notifications = inject(NotificationsApiService);
  readonly unreadCount$ = this.notifications.unreadCount$;
  private notificationSub?: Subscription;

  ngOnInit(): void {
    this.realtime.start();
    this.loadNotificationCount();
    this.notificationSub = this.realtime.notifications.subscribe(() => this.loadNotificationCount());
  }
  ngOnDestroy(): void { this.notificationSub?.unsubscribe(); void this.realtime.stop(); }
  private loadNotificationCount(): void {
    this.notifications.getAll().subscribe({ error: () => this.notifications.setUnreadCount(0) });
  }
}
