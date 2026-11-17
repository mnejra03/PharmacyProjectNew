import { Component, OnInit, inject } from '@angular/core';
import { AdminNotificationItem, NotificationsApiService } from '../../../api-services/notifications/notifications-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-admin-notifications',
  standalone: false,
  template: `
    <section class="page admin-notifications">
      <header><p class="eyebrow">Administracija</p><h1>Sve obavijesti</h1><p>Pregled obavijesti poslanih korisnicima, farmaceutima i administratorima.</p></header>
      <p *ngIf="!items.length" class="empty-state">Trenutno nema obavijesti.</p>
      <mat-card *ngFor="let item of items" class="notification-card" [class.unread]="!item.isRead">
        <mat-card-header>
          <mat-card-title>{{item.title}} <span *ngIf="!item.isRead" class="new-label">Nepročitano za vas</span></mat-card-title>
          <mat-card-subtitle>{{item.createdAt | date:'short'}}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content><div class="notification-parties"><span><strong>Od:</strong> {{item.senderName || 'Sistem'}}<small *ngIf="item.senderEmail">{{item.senderEmail}}</small></span><span><strong>Za:</strong> {{item.recipientName}}<small>{{item.recipientEmail}}</small></span><span><strong>Status kod primaoca:</strong> {{item.recipientIsRead ? 'Pročitano' : 'Nepročitano'}}</span><span><strong>Vaš admin status:</strong> {{item.isRead ? 'Pročitano' : 'Nepročitano'}}</span></div><p class="message">{{displayMessage(item)}}</p></mat-card-content>
        <mat-card-actions><button mat-button (click)="setReadState(item,!item.isRead)">{{item.isRead?'Označi kao nepročitano za mene':'Označi kao pročitano za mene'}}</button></mat-card-actions>
      </mat-card>
    </section>
  `,
  styles: [`
    .admin-notifications{max-width:1000px;margin:0 auto;padding:1.5rem}
    header{margin-bottom:1.25rem}.eyebrow{margin:0;color:#287342;font-size:.8rem;font-weight:700;text-transform:uppercase;letter-spacing:.08em}
    h1{margin:.25rem 0;color:#174d2b}.notification-card{margin:.85rem 0;border-left:5px solid #98a2b3}
    .notification-card.unread{border-left-color:#ef2929;background:#fffafa}.new-label{display:inline-block;margin-left:.5rem;padding:.15rem .5rem;border-radius:999px;background:#ef2929;color:#fff;font-size:.7rem;vertical-align:middle}.notification-parties{display:flex;flex-wrap:wrap;gap:1rem;margin:.25rem 0 .9rem}.notification-parties>span{display:grid;gap:.15rem;min-width:220px}.notification-parties small{color:#65756f}.message{margin:0;line-height:1.5}
    .empty-state{padding:2rem;border-radius:12px;background:#fff;color:#526458;text-align:center}
  `]
})
export class AdminNotificationsComponent implements OnInit {
  private readonly api = inject(NotificationsApiService);
  private readonly toaster = inject(ToasterService);
  items: AdminNotificationItem[] = [];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.getAllForAdmin().subscribe({ next: items => this.items = items, error: () => this.toaster.error('Sve obavijesti nije moguće učitati.') });
  }

  displayMessage(item: AdminNotificationItem): string {
    if (item.message.includes('#0') && item.message.toLowerCase().includes('narud')) return item.message.replace(/\s+#0\b/g, '');
    return item.message;
  }

  setReadState(item: AdminNotificationItem, isRead: boolean): void {
    this.api.setAdminReadState(item.id, isRead).subscribe({
      next: () => {
        item.isRead = isRead;
        this.api.setUnreadCount(this.items.filter(notification => !notification.isRead).length);
        this.toaster.success(isRead ? 'Označeno kao pročitano za vaš admin pregled.' : 'Označeno kao nepročitano za vaš admin pregled.');
      },
      error: () => this.toaster.error('Status obavijesti nije moguće promijeniti.')
    });
  }
}
