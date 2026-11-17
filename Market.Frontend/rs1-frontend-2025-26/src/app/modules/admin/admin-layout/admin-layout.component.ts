import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { NotificationsApiService } from '../../../api-services/notifications/notifications-api.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';
import { Subscription } from 'rxjs';
import { UsersApiService } from '../../../api-services/users/users-api.service';

@Component({
  selector: 'app-admin-layout',
  standalone: false,
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  private translate = inject(TranslateService);
  auth = inject(AuthFacadeService);
  private notifications = inject(NotificationsApiService);
  private users = inject(UsersApiService);
  private realtime = inject(ChatRealtimeService);
  readonly unreadCount$ = this.notifications.unreadCount$;
  private notificationSub?: Subscription;
  adminName = '';

  currentLang: string;

  languages = [
    { code: 'bs', name: 'Bosanski', flag: '🇧🇦' },
    { code: 'en', name: 'English', flag: '🇬🇧' }
  ];

  constructor() {
    this.currentLang = this.translate.currentLang || 'bs';
  }

  ngOnInit(): void {
    this.realtime.start();
    this.users.getMe().subscribe({ next: profile => this.adminName = `${profile.firstName} ${profile.lastName}`.trim() });
    this.loadNotificationCount();
    this.notificationSub = this.realtime.notifications.subscribe(() => this.loadNotificationCount());
  }

  ngOnDestroy(): void {
    this.notificationSub?.unsubscribe();
    void this.realtime.stop();
  }

  private loadNotificationCount(): void {
    this.notifications.getAllForAdmin().subscribe({
      next: items => this.notifications.setUnreadCount(items.filter(item => !item.isRead).length),
      error: () => this.notifications.setUnreadCount(0)
    });
  }

  switchLanguage(langCode: string): void {
    this.currentLang = langCode;
    this.translate.use(langCode);
    localStorage.setItem('language', langCode);
  }

  getCurrentLanguage() {
    return this.languages.find(lang => lang.code === this.currentLang);
  }
}
