import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';

@Component({
  selector: 'app-client-layout',
  standalone: false,
  template: `<nav class="client-nav">
    <a mat-button routerLink="/catalog">Proizvodi</a>
    <a mat-button routerLink="/client/cart">Korpa</a>
    <a mat-button routerLink="/client/wishlist">Favoriti</a>
    <a mat-button routerLink="/client/orders">Narudžbe</a>
    <a mat-button routerLink="/client/profile">Profil</a>
    <a mat-button routerLink="/client/recipes">Recepti</a>
    <a mat-button *ngIf="auth.isPharmacist() || auth.isAdmin()" routerLink="/client/pharmacist/recipes">Pregled recepata</a>
    <a mat-button *ngIf="auth.isPharmacist()" routerLink="/client/supply-orders">Nabavka i zalihe</a>
    <a mat-button routerLink="/client/chat">Razgovor</a>
    <a mat-button routerLink="/client/notifications">Obavijesti</a>
    <a mat-button routerLink="/auth/logout">Odjava</a>
  </nav><router-outlet></router-outlet>`
})
export class ClientLayoutComponent implements OnInit, OnDestroy {
  auth = inject(AuthFacadeService);
  private realtime = inject(ChatRealtimeService);

  ngOnInit(): void { this.realtime.start(); }
  ngOnDestroy(): void { void this.realtime.stop(); }
}
