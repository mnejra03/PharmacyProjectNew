import { Component, OnInit, inject } from '@angular/core';
import { PharmacyOrder, PharmacySupplyOrder, StoreApiService } from '../../../api-services/store/store-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-admin-orders',
  standalone: false,
  template: `
    <section class="orders-page">
      <header><p class="eyebrow">Prodaja</p><h1>Narudžbe</h1><p>Pregled svih narudžbi i ažuriranje njihovog statusa.</p></header>
      <p *ngIf="loading" class="state">Učitavam narudžbe…</p>
      <p *ngIf="errorMessage" class="state error">{{errorMessage}} <button mat-button (click)="load()">Pokušaj ponovo</button></p>
      <div *ngIf="!loading && !errorMessage && !orders.length" class="empty-state">
        <mat-icon>receipt_long</mat-icon><h2>Nema narudžbi</h2><p>Narudžbe će se pojaviti ovdje nakon što kupac završi checkout.</p>
      </div>
      <mat-card class="order-card" *ngFor="let order of orders">
        <mat-card-header><mat-card-title>Narudžba #{{order.id}}</mat-card-title><mat-card-subtitle>{{formatSarajevoTime(order.orderedAtUtc)}}</mat-card-subtitle></mat-card-header>
        <mat-card-content>
          <div class="order-meta"><span><b>Kupac:</b> {{order.customerName || 'Korisnik'}} · {{order.customerEmail}}</span><span><b>Adresa:</b> {{order.shippingAddress}}</span><span><b>Plaćanje:</b> {{order.paymentMethod}}</span></div>
          <div class="item-line" *ngFor="let item of order.items"><span>{{item.name}} × {{item.quantity}}</span><span>{{item.unitPrice * item.quantity | number:'1.2-2'}} KM</span></div>
          <div class="order-footer"><span class="status-pill" [ngClass]="'status-'+order.status.toLowerCase()">{{statusLabel(order.status)}}</span><strong>Ukupno: {{order.totalPrice | number:'1.2-2'}} KM</strong><mat-form-field appearance="outline"><mat-label>Status</mat-label><mat-select [value]="order.status" (selectionChange)="updateStatus(order,$event.value)"><mat-option *ngFor="let status of statuses" [value]="status">{{statusLabel(status)}}</mat-option></mat-select></mat-form-field></div>
        </mat-card-content>
      </mat-card>
      <header class="supply-header"><p class="eyebrow">Nabavka</p><h2>Zahtjevi za dopunu zaliha</h2><p>Odobrenjem zahtjeva tražena količina se dodaje na stanje proizvoda.</p></header>
      <div *ngIf="!supplyOrders.length" class="empty-state"><p>Nema zahtjeva za nabavku.</p></div>
      <mat-card class="order-card" *ngFor="let order of supplyOrders">
        <mat-card-header><mat-card-title>Zahtjev #{{order.id}} · {{order.pharmacistName}}</mat-card-title><mat-card-subtitle>{{formatSarajevoTime(order.orderedAtUtc)}} · {{order.pharmacistEmail}}</mat-card-subtitle></mat-card-header>
        <mat-card-content>
          <div class="item-line" *ngFor="let item of order.items"><span>{{item.name}} × {{item.quantity}}</span><span>{{item.unitPrice * item.quantity | number:'1.2-2'}} KM</span></div>
          <div class="order-footer"><span class="status-pill" [ngClass]="'status-'+order.status.toLowerCase()">{{statusLabel(order.status)}}</span><strong>Ukupno: {{order.totalPrice | number:'1.2-2'}} KM</strong></div>
          <div class="supply-actions" *ngIf="order.status === 'Pending'"><button mat-raised-button color="primary" (click)="reviewSupply(order,'Approved')">Odobri i zaprimi</button><button mat-button color="warn" (click)="reviewSupply(order,'Rejected')">Odbij</button></div>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [`
    .orders-page{max-width:1100px;margin:0 auto;padding:1.5rem;color:#213629}header{margin-bottom:1.5rem}header h1{margin:.2rem 0;color:#1b5e20}header p{color:#526458}.supply-header{margin-top:2.5rem}.supply-header h2{color:#1b5e20}.eyebrow{margin:0;color:#2e7d32;font-size:.8rem;font-weight:700;letter-spacing:.08em;text-transform:uppercase}.order-card{margin-bottom:1rem;border:1px solid #dce6df;border-radius:12px}.order-meta{display:grid;gap:.4rem;padding:.5rem 0 1rem;color:#43584a}.item-line{display:flex;justify-content:space-between;gap:1rem;padding:.6rem 0;border-top:1px solid #edf1ee}.order-footer{display:flex;align-items:center;justify-content:space-between;gap:1rem;margin-top:.75rem;padding-top:.75rem;border-top:1px solid #dce6df;color:#1b5e20}.order-footer mat-form-field{width:210px;margin:0}.supply-actions{display:flex;gap:.5rem;margin-top:1rem}.status-pill{display:inline-flex;align-items:center;padding:.4rem .75rem;border-radius:999px;font-size:.82rem;font-weight:800;letter-spacing:.02em}.status-pending{background:#fff1bf;color:#7a4b00}.status-processing{background:#dbeafe;color:#174ea6}.status-shipped{background:#ede9fe;color:#5b21b6}.status-completed,.status-approved{background:#d1fae5;color:#065f46}.status-rejected,.status-cancelled{background:#fee2e2;color:#991b1b}.empty-state,.state{padding:2rem;border:1px solid #dce6df;border-radius:12px;background:#fff;text-align:center;color:#43584a}.empty-state mat-icon{width:44px;height:44px;color:#2e7d32;font-size:44px}.empty-state h2{color:#1b5e20}.error{color:#a31d24}@media(max-width:650px){.orders-page{padding:1rem .5rem}.order-footer{align-items:flex-start;flex-direction:column}.order-footer mat-form-field{width:100%}}
  `]
})
export class AdminOrdersComponent implements OnInit {
  private readonly api = inject(StoreApiService);
  private readonly toaster = inject(ToasterService);
  readonly statuses = ['Pending', 'Processing', 'Shipped', 'Completed', 'Cancelled'];
  statusLabel(status: string): string { return ({ Pending: 'Na čekanju', Processing: 'U obradi', Shipped: 'Poslano', Completed: 'Završeno', Cancelled: 'Otkazano' } as Record<string, string>)[status] ?? status; }
  formatSarajevoTime(value: string): string { return new Intl.DateTimeFormat('bs-BA', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Europe/Sarajevo' }).format(new Date(value)); }
  orders: PharmacyOrder[] = [];
  supplyOrders: PharmacySupplyOrder[] = [];
  loading = false;
  errorMessage = '';

  ngOnInit(): void { this.load(); this.loadSupplyOrders(); }
  load(): void {
    this.loading = true; this.errorMessage = '';
    this.api.getOrders(true).subscribe({
      next: orders => { this.orders = orders; this.loading = false; },
      error: () => { this.orders = []; this.loading = false; this.errorMessage = 'Narudžbe nije moguće učitati. Provjerite administratorsku prijavu.'; }
    });
  }
  updateStatus(order: PharmacyOrder, status: string): void {
    this.api.updateOrderStatus(order.id, status).subscribe({
      next: () => { order.status = status; this.toaster.success('Status narudžbe je izmijenjen.'); },
      error: () => this.toaster.error('Status narudžbe nije moguće izmijeniti.')
    });
  }

  private loadSupplyOrders(): void {
    this.api.getSupplyOrders(true).subscribe({
      next: orders => this.supplyOrders = orders,
      error: () => this.toaster.error('Zahtjeve za nabavku nije moguće učitati.')
    });
  }

  reviewSupply(order: PharmacySupplyOrder, status: 'Approved' | 'Rejected'): void {
    this.api.updateSupplyOrderStatus(order.id, status).subscribe({
      next: () => { order.status = status; this.toaster.success(status === 'Approved' ? 'Zahtjev je odobren i zaliha je zaprimljena.' : 'Zahtjev je odbijen.'); },
      error: () => this.toaster.error('Status zahtjeva nije moguće promijeniti.')
    });
  }
}
