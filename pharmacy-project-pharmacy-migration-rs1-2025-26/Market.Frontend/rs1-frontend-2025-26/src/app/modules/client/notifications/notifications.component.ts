import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { NotificationsApiService, NotificationItem } from '../../../api-services/notifications/notifications-api.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-notifications',
  standalone: false,
  template: `<section class="page notifications-page"><h1>Obavijesti</h1><p *ngIf="!items.length" class="empty">Trenutno nema obavijesti.</p><mat-card *ngFor="let item of items" class="notice" [class.unread]="!item.isRead"><mat-card-header><mat-card-title>{{item.title}} <span *ngIf="!item.isRead" class="unread-label">Novo</span></mat-card-title><mat-card-subtitle>{{item.createdAt | date:'short'}}</mat-card-subtitle></mat-card-header><mat-card-content>{{item.message}}</mat-card-content><mat-card-actions><button mat-button *ngIf="!item.isRead" (click)="markRead(item)">Označi pročitano</button><button mat-button color="warn" (click)="remove(item)">Obriši</button></mat-card-actions></mat-card></section>`,
  styles: [`.notifications-page{max-width:900px;margin:0 auto;padding:1.5rem}.notifications-page h1{color:#174ea6}.notice{margin:.85rem 0;border-left:5px solid #98a2b3}.notice.unread{border-left-color:#f79009;background:#fffaeb}.unread-label{display:inline-block;margin-left:.45rem;padding:.2rem .5rem;border-radius:999px;background:#fdb022;color:#422006;font-size:.72rem;font-weight:800}.empty{padding:2rem;border-radius:12px;background:#fff;color:#526458}`]
})
export class NotificationsComponent implements OnInit, OnDestroy {
  private readonly api = inject(NotificationsApiService);
  private readonly realtime = inject(ChatRealtimeService);
  private readonly toaster = inject(ToasterService);
  items: NotificationItem[] = [];
  private sub?: Subscription;

  ngOnInit(): void {
    this.load();
    this.sub = this.realtime.notifications.subscribe(item => {
      this.items = [item, ...this.items.filter(existing => existing.id !== item.id)];
      this.updateUnreadCount();
    });
  }
  ngOnDestroy(): void { this.sub?.unsubscribe(); }
  load(): void { this.api.getAll().subscribe({ next: items => this.items = items, error: () => this.toaster.error('Obavijesti nije moguće učitati.') }); }
  markRead(item: NotificationItem): void { this.api.markRead(item.id).subscribe({ next: () => { item.isRead = true; this.updateUnreadCount(); this.toaster.success('Obavijest je označena kao pročitana.'); }, error: () => this.toaster.error('Obavijest nije moguće ažurirati.') }); }
  remove(item: NotificationItem): void { this.api.delete(item.id).subscribe({ next: () => { this.items = this.items.filter(existing => existing.id !== item.id); this.updateUnreadCount(); this.toaster.success('Obavijest je obrisana.'); }, error: () => this.toaster.error('Obavijest nije moguće obrisati.') }); }
  private updateUnreadCount(): void { this.api.setUnreadCount(this.items.filter(item => !item.isRead).length); }
}
