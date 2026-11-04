import { HttpClient } from '@angular/common/http'; import { inject, Injectable } from '@angular/core'; import { environment } from '../../../environments/environment';
export interface DashboardStats { users:number; pharmacists:number; recipes:number; pendingRecipes:number; unreadNotifications:number; }
@Injectable({providedIn:'root'}) export class DashboardApiService { private http=inject(HttpClient); getStats(){return this.http.get<DashboardStats>(`${environment.apiUrl}/api/dashboard/stats`);} }
