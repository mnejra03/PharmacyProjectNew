import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { UsersApiService, UserProfile } from '../../../api-services/users/users-api.service';

@Component({
  selector: 'app-users', standalone: false,
  template: `<section class="page"><h1>Korisnici</h1>
    <form [formGroup]="filters" (ngSubmit)="search()">
      <mat-form-field><mat-label>Pretraga</mat-label><input matInput formControlName="search"></mat-form-field>
      <mat-form-field><mat-label>Uloga</mat-label><mat-select formControlName="role"><mat-option value="">Sve</mat-option><mat-option value="admin">Administrator</mat-option><mat-option value="pharmacist">Farmaceut</mat-option><mat-option value="customer">Kupac</mat-option></mat-select></mat-form-field>
      <button mat-raised-button type="submit">Filtriraj</button>
    </form>
    <table mat-table [dataSource]="items">
      <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Ime</th><td mat-cell *matCellDef="let u">{{u.firstName}} {{u.lastName}}</td></ng-container>
      <ng-container matColumnDef="email"><th mat-header-cell *matHeaderCellDef>Email</th><td mat-cell *matCellDef="let u">{{u.email}}</td></ng-container>
      <ng-container matColumnDef="role"><th mat-header-cell *matHeaderCellDef>Uloge</th><td mat-cell *matCellDef="let u">{{roleNames(u)}}</td></ng-container>
      <ng-container matColumnDef="actions"><th mat-header-cell *matHeaderCellDef>Akcije</th><td mat-cell *matCellDef="let u"><button mat-button (click)="edit(u)">Uredi</button><button mat-button color="warn" (click)="remove(u.id)">Ukloni</button></td></ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr><tr mat-row *matRowDef="let row;columns:columns"></tr>
      <tr class="mat-row" *matNoDataRow><td class="mat-cell" [attr.colspan]="columns.length">{{loading?'Učitavam korisnike…':'Nema korisnika za odabrane filtere.'}}</td></tr>
    </table><p *ngIf="errorMessage" class="error-message">{{errorMessage}} <button mat-button type="button" (click)="load()">Pokušaj ponovo</button></p><p>Ukupno: {{totalItems}}</p>
    <form *ngIf="selected" class="user-edit-form" [formGroup]="editForm" (ngSubmit)="save()"><h2>Uredi {{selected.email}}</h2>
      <mat-form-field><mat-label>Ime</mat-label><input matInput formControlName="firstName"></mat-form-field><mat-form-field><mat-label>Prezime</mat-label><input matInput formControlName="lastName"></mat-form-field><mat-form-field><mat-label>Telefon</mat-label><input matInput formControlName="phoneNumber"></mat-form-field>
      <fieldset class="role-options"><legend>Uloge korisnika</legend><mat-checkbox formControlName="isAdmin">Administrator</mat-checkbox><mat-checkbox formControlName="isPharmacist">Farmaceut</mat-checkbox><mat-checkbox formControlName="isCustomer">Kupac</mat-checkbox><small>Možete zadržati više uloga. Najmanje jedna uloga mora biti označena.</small></fieldset>
      <p *ngIf="saveError" class="error-message save-error" role="alert">{{saveError}}</p>
      <div class="edit-actions"><button mat-raised-button color="primary" type="submit" [disabled]="saving">{{saving?'Spremam…':'Sačuvaj'}}</button><button mat-button type="button" [disabled]="saving" (click)="cancelEdit()">Odustani</button></div>
    </form>
    <button mat-button (click)="previous()" [disabled]="page<=1">Prethodna</button><span> {{page}} / {{totalPages}} </span><button mat-button (click)="next()" [disabled]="page>=totalPages">Sljedeća</button>
  </section>`,
  styles: [`
    .user-edit-form{display:grid;grid-template-columns:repeat(2,minmax(220px,1fr));gap:.25rem 1rem;max-width:760px;margin:1.5rem 0;padding:1.25rem;border:1px solid #dce6df;border-radius:12px;background:#fff}
    .user-edit-form h2,.role-options,.save-error,.edit-actions{grid-column:1/-1}
    .user-edit-form mat-form-field{width:100%}
    .role-options{display:flex;flex-wrap:wrap;gap:.6rem 1.25rem;margin:.25rem 0 1rem;padding:.75rem 1rem 1rem;border:1px solid #dce6df;border-radius:8px}
    .role-options legend{padding:0 .35rem;font-weight:600;color:#244b31}
    .role-options small{flex-basis:100%;color:#617167}
    .save-error{margin:0 0 .75rem;color:#b42318}
    .edit-actions{display:flex;gap:.5rem}
    @media(max-width:600px){.user-edit-form{grid-template-columns:1fr;padding:1rem}.user-edit-form h2,.role-options,.save-error,.edit-actions{grid-column:1}}
  `]
})
export class UsersComponent implements OnInit {
  private fb = inject(FormBuilder); private api = inject(UsersApiService);
  filters = this.fb.group({ search: ['', [Validators.maxLength(200)]], role: ['', [Validators.maxLength(30)]] }); editForm=this.fb.group({firstName:['',[Validators.required,Validators.maxLength(100)]],lastName:['',[Validators.required,Validators.maxLength(100)]],phoneNumber:['',[Validators.maxLength(30)]],isAdmin:[false],isPharmacist:[false],isCustomer:[true]}); items: UserProfile[] = []; selected?:UserProfile;
  columns = ['name', 'email', 'role','actions']; page = 1; pageSize = 20; totalItems = 0; totalPages = 1; loading=false; errorMessage='';saveError='';saving=false;
  ngOnInit(): void {
    this.load();
    this.filters.controls.role.valueChanges.subscribe(() => this.search());
  }
  search(): void { this.page = 1; this.load(); }
  previous(): void { if (this.page > 1) { this.page--; this.load(); } }
  next(): void { if (this.page < this.totalPages) { this.page++; this.load(); } }
  edit(user:UserProfile):void {
    this.selected=user;
    this.saveError='';
    this.editForm.patchValue({ firstName:user.firstName,lastName:user.lastName,phoneNumber:user.phoneNumber??'',isAdmin:user.isAdmin,isPharmacist:user.isPharmacist,isCustomer:user.isCustomer });
  }
  roleNames(user:UserProfile):string { return [[user.isAdmin,'Administrator'],[user.isPharmacist,'Farmaceut'],[user.isCustomer,'Kupac']].filter(([enabled])=>enabled).map(([,name])=>name as string).join(', ')||'—'; }
  cancelEdit():void { this.selected=undefined;this.saveError=''; }
  save():void {
    if(!this.selected||this.saving)return;
    if(this.editForm.invalid){this.editForm.markAllAsTouched();return;}
    const v=this.editForm.getRawValue();
    this.saving=true;this.saveError='';
    this.api.updateUser(this.selected.id,{
      firstName:v.firstName??'',lastName:v.lastName??'',phoneNumber:v.phoneNumber??undefined,
      isAdmin:!!v.isAdmin,isPharmacist:!!v.isPharmacist,isCustomer:!!v.isCustomer
    }).subscribe({next:()=>{this.saving=false;this.selected=undefined;this.load();},error:error=>{this.saving=false;this.saveError=this.apiErrorMessage(error,'Izmjene korisnika nisu sačuvane. Provjerite podatke i pokušajte ponovo.');}});
  }
  private apiErrorMessage(error:any,fallback:string):string { const body=error?.error;const message=typeof body==='string'?body:body?.message??body?.Message??body?.title;return typeof message==='string'&&message.trim()?message:fallback; }
  remove(id:number):void {if(confirm('Ukloniti ovog korisnika?'))this.api.deleteUser(id).subscribe(()=>this.load());}
  load(): void {
    this.loading=true; this.errorMessage='';
    this.api.getUsers({ page: this.page, pageSize: this.pageSize, search: this.filters.value.search || undefined, role: this.filters.value.role || undefined })
      .subscribe({next:result => { this.items = result.items; this.totalItems = result.totalItems; this.totalPages = Math.max(1, result.totalPages); this.loading=false; },error:error=>{this.items=[];this.totalItems=0;this.totalPages=1;this.loading=false;this.errorMessage=error?.error?.message||error?.error?.details||'Korisnike nije moguće učitati. Provjerite administratorsku prijavu i pokušajte ponovo.';}});
  }
}
