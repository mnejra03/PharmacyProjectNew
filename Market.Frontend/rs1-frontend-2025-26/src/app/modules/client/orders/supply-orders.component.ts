import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { PharmacySupplyOrder, Product, StoreApiService } from '../../../api-services/store/store-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-supply-orders',
  standalone: false,
  template: `
    <section class="page supply-page">
      <h1>Nabavka i zalihe</h1>
      <p>Pošalji administratoru zahtjev za dopunu zaliha. Stanje proizvoda se poveća tek nakon odobrenja.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline"><mat-label>Proizvod</mat-label>
          <mat-select formControlName="productId">
            <mat-option *ngFor="let product of products" [value]="product.id">{{product.name}} · zaliha {{product.quantityInStock}} · {{product.price | number:'1.2-2'}} KM</mat-option>
          </mat-select>
          <mat-error *ngIf="form.controls.productId.invalid">Odaberi proizvod.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Količina</mat-label><input matInput type="number" min="1" max="100000" formControlName="quantity"></mat-form-field>
        <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting">{{submitting?'Šaljem…':'Pošalji zahtjev'}}</button>
      </form>

      <h2>Moji zahtjevi</h2>
      <p *ngIf="loading">Učitavam zahtjeve…</p>
      <p *ngIf="!loading && !orders.length">Još nema zahtjeva za nabavku.</p>
      <mat-card *ngFor="let order of orders" class="supply-card">
        <mat-card-header><mat-card-title>Zahtjev #{{order.id}}</mat-card-title>
          <mat-card-subtitle>{{order.orderedAtUtc | date:'medium'}} · <span class="status-pill" [ngClass]="'status-'+order.status.toLowerCase()">{{statusLabel(order.status)}}</span></mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p *ngFor="let item of order.items">{{item.name}} × {{item.quantity}} — {{item.unitPrice * item.quantity | number:'1.2-2'}} KM</p>
          <strong>Ukupno: {{order.totalPrice | number:'1.2-2'}} KM</strong>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [`.supply-page{max-width:1000px;margin:auto}.supply-page form{display:flex;flex-wrap:wrap;align-items:center;gap:1rem}.supply-page form mat-form-field{flex:1 1 250px}.supply-card{margin:1rem 0}.status-pill{display:inline-block;padding:.25rem .65rem;border-radius:999px;background:#fff1bf;color:#7a4b00;font-weight:700}.status-approved{background:#d1fae5;color:#065f46}.status-rejected{background:#fee2e2;color:#991b1b}`]
})
export class SupplyOrdersComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(StoreApiService);
  private readonly toaster = inject(ToasterService);
  readonly form = this.fb.group({ productId: [null as number | null, Validators.required], quantity: [1, [Validators.required, Validators.min(1), Validators.max(100000)]] });
  products: Product[] = [];
  orders: PharmacySupplyOrder[] = [];
  loading = false;
  submitting = false;

  ngOnInit(): void {
    this.api.getProducts({ page: 1, pageSize: 100 }).subscribe({ next: result => this.products = result.items, error: () => this.toaster.error('Proizvode nije moguće učitati.') });
    this.load();
  }

  statusLabel(status: string): string { return ({ Pending: 'Na čekanju', Approved: 'Odobreno', Rejected: 'Odbijeno' } as Record<string, string>)[status] ?? status; }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const { productId, quantity } = this.form.getRawValue();
    if (productId == null || quantity == null) return;
    this.submitting = true;
    this.api.createSupplyOrder(productId, quantity).subscribe({
      next: () => { this.submitting = false; this.form.reset({ productId: null, quantity: 1 }); this.load(); this.toaster.success('Zahtjev za nabavku poslan je administratoru.'); },
      error: () => { this.submitting = false; this.toaster.error('Zahtjev za nabavku nije moguće poslati.'); }
    });
  }

  private load(): void {
    this.loading = true;
    this.api.getSupplyOrders().subscribe({
      next: orders => { this.orders = orders; this.loading = false; },
      error: () => { this.orders = []; this.loading = false; this.toaster.error('Zahtjeve za nabavku nije moguće učitati.'); }
    });
  }
}
