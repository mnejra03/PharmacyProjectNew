import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
export interface UserProfile { id: number; email: string; firstName: string; lastName: string; phoneNumber?: string; isAdmin: boolean; isPharmacist: boolean; isCustomer: boolean; profileImageUrl?: string; }
export interface ChangePassword { currentPassword: string; newPassword: string; }
export interface UserPage { items: UserProfile[]; pageSize: number; currentPage: number; totalItems: number; totalPages: number; includedTotal: boolean; }
export interface UpdateUser { firstName:string; lastName:string; phoneNumber?:string; isAdmin:boolean; isPharmacist:boolean; isCustomer:boolean; }
@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private http = inject(HttpClient); private url = `${environment.apiUrl}/api/users`;
  getMe() { return this.http.get<UserProfile>(`${this.url}/me`); }
  updateMe(body: Pick<UserProfile, 'firstName' | 'lastName' | 'phoneNumber'>) { return this.http.put<UserProfile>(`${this.url}/me`, body); }
  changePassword(body: ChangePassword) { return this.http.post<void>(`${this.url}/me/password`, body); }
  updateProfileImage(file:File){const body=new FormData();body.append('file',file);return this.http.post<UserProfile>(`${this.url}/me/profile-image`,body);}
  getUsers(params: { page: number; pageSize: number; search?: string; role?: string }) {
    let query = new HttpParams().set('page', params.page).set('pageSize', params.pageSize);
    if (params.search?.trim()) query = query.set('search', params.search.trim());
    if (params.role?.trim()) query = query.set('role', params.role.trim().toLowerCase());
    return this.http.get<UserPage>(this.url, { params: query });
  }
  updateUser(id:number, body:UpdateUser){return this.http.put<UserProfile>(`${this.url}/${id}`,body);}
  deleteUser(id:number){return this.http.delete<void>(`${this.url}/${id}`);}
}
