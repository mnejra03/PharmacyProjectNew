import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthFacadeService } from './auth/auth-facade.service';
import { ChatMessage } from '../../api-services/chat/chat-api.service';
import { NotificationItem } from '../../api-services/notifications/notifications-api.service';

@Injectable({ providedIn: 'root' })
export class ChatRealtimeService {
  private auth = inject(AuthFacadeService);
  private connection?: HubConnection;
  readonly messages = new Subject<ChatMessage>();
  readonly notifications = new Subject<NotificationItem>();

  start(): void {
    if (this.connection || !this.auth.getAccessToken()) return;
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/chat`, { accessTokenFactory: () => this.auth.getAccessToken() ?? '' })
      .withAutomaticReconnect()
      .build();
    this.connection.on('ReceiveMessage', (message: ChatMessage) => this.messages.next(message));
    this.connection.on('ReceiveNotification', (item: NotificationItem) => this.notifications.next(item));
    void this.connection.start().catch(() => { this.connection = undefined; });
  }

  async stop(): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Disconnected) await this.connection?.stop();
    this.connection = undefined;
  }
}
