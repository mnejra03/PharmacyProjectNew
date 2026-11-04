import { HttpClient } from '@angular/common/http'; import { inject, Injectable } from '@angular/core'; import { BehaviorSubject, tap } from 'rxjs'; import { environment } from '../../../environments/environment';
export interface NotificationItem { id: number; title: string; message: string; createdAt: string; isRead: boolean; type: string; senderId?: number; }
@Injectable({ providedIn: 'root' }) export class NotificationsApiService {
  private http=inject(HttpClient); private url=`${environment.apiUrl}/api/notifications`;
  private unreadCountSubject=new BehaviorSubject<number>(0);
  readonly unreadCount$=this.unreadCountSubject.asObservable();
  getAll(){return this.http.get<NotificationItem[]>(this.url).pipe(tap(items=>this.setUnreadCount(items.filter(item=>!item.isRead).length)));}
  setUnreadCount(count:number){this.unreadCountSubject.next(count);}
  markRead(id:number){return this.http.put<void>(`${this.url}/${id}/read`,{});}
  delete(id:number){return this.http.delete<void>(`${this.url}/${id}`);}
}
