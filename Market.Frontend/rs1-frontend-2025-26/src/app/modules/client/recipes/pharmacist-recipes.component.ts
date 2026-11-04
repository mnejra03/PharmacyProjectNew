import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RecipesApiService, Recipe } from '../../../api-services/recipes/recipes-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-pharmacist-recipes',
  standalone: false,
  template: `
    <section class="recipes-page">
      <header class="page-header">
        <div><span class="eyebrow">APOTEKA - OBRADA</span><h1>Recepti za pregled</h1>
          <p>Pristigli recepti kupaca i njihov trenutni status.</p></div>
        <span class="live-indicator"><i></i> Automatsko osvjezavanje</span>
      </header>
      <div class="summary-grid">
        <button type="button" class="summary-card" [class.selected]="filter==='All'" (click)="filter='All'"><span>Svi recepti</span><strong>{{items.length}}</strong></button>
        <button type="button" class="summary-card pending" [class.selected]="filter==='Pending'" (click)="filter='Pending'"><span>Cekaju pregled</span><strong>{{count('Pending')}}</strong></button>
        <button type="button" class="summary-card approved" [class.selected]="filter==='Approved'" (click)="filter='Approved'"><span>Odobreni</span><strong>{{count('Approved')}}</strong></button>
        <button type="button" class="summary-card rejected" [class.selected]="filter==='Rejected'" (click)="filter='Rejected'"><span>Odbijeni</span><strong>{{count('Rejected')}}</strong></button>
      </div>
      <p class="empty-state" *ngIf="!filteredItems.length">{{items.length ? 'Nema recepata u ovoj kategoriji.' : 'Jos nema pristiglih recepata.'}}</p>
      <article class="recipe-card" *ngFor="let item of filteredItems"
        [class.status-pending]="item.status==='Pending'"
        [class.status-approved]="item.status==='Approved'"
        [class.status-rejected]="item.status==='Rejected'">
        <div class="card-top">
          <div class="sender"><span class="avatar">{{initials(item.senderName)}}</span>
            <div><span class="label">POSLAO/LA</span><h2>{{item.senderName || 'Korisnik #' + item.userId}}</h2>
              <a [href]="'mailto:' + item.senderEmail">{{item.senderEmail}}</a></div></div>
          <span class="status-pill" [class.pill-pending]="item.status==='Pending'" [class.pill-approved]="item.status==='Approved'" [class.pill-rejected]="item.status==='Rejected'">{{statusLabel(item.status)}}</span>
        </div>
        <div class="details-grid">
          <div><span class="label">LJEKAR NA RECEPTU</span><strong>Dr. {{item.doctorFirstName}} {{item.doctorLastName}}</strong></div>
          <div><span class="label">DATUM PRIJEMA</span><strong>{{item.dateOfIssue | date:'d. M. y. - HH:mm'}}</strong></div>
          <div><span class="label">BROJ RECEPTA</span><strong>#{{item.id}}</strong></div>
        </div>
        <footer class="card-actions">
          <button mat-stroked-button *ngIf="item.hasScan" (click)="download(item)">Pregledaj sken</button>
          <span class="decision-note" *ngIf="item.status==='Approved'">Recept je odobren</span>
          <span class="decision-note rejected-note" *ngIf="item.status==='Rejected'">Recept je odbijen</span>
          <span class="action-spacer"></span>
          <ng-container *ngIf="item.status==='Pending'">
            <button mat-button class="reject-button" (click)="setStatus(item,'Rejected')">Odbij</button>
            <button mat-flat-button color="primary" (click)="setStatus(item,'Approved')">Odobri recept</button>
          </ng-container>
        </footer>
      </article>
    </section>
  `,
  styles: [`
    .recipes-page{max-width:1080px;margin:32px auto;padding:0 24px 48px;color:#18332d}
    .page-header{display:flex;align-items:center;justify-content:space-between;gap:20px;margin-bottom:24px}
    .eyebrow,.label{display:block;color:#71817b;font-size:11px;font-weight:700;letter-spacing:.09em}
    h1{margin:6px 0;font-size:30px}.page-header p{margin:0;color:#64746e}
    .live-indicator{white-space:nowrap;color:#52665e;font-size:13px}.live-indicator i{display:inline-block;width:8px;height:8px;margin-right:6px;border-radius:50%;background:#28a477}
    .summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;margin:22px 0}
    .summary-card{display:flex;justify-content:space-between;align-items:center;padding:16px;border:1px solid #e1e9e5;border-radius:12px;background:#fff;color:#4d6259;text-align:left;cursor:pointer}
    .summary-card strong{font-size:23px;color:#18332d}.summary-card.selected{border-color:#29846c;box-shadow:0 0 0 2px #29846c18}
    .summary-card.pending strong{color:#a96a08}.summary-card.approved strong{color:#21805e}.summary-card.rejected strong{color:#b44747}
    .recipe-card{margin:14px 0;padding:20px;border:1px solid #e2e9e6;border-left:4px solid #d5a33c;border-radius:14px;background:white}
    .recipe-card.status-approved{border-left-color:#29846c}.recipe-card.status-rejected{border-left-color:#c75c58}
    .card-top{display:flex;justify-content:space-between;align-items:center;gap:16px}
    .sender{display:flex;align-items:center;gap:13px}.avatar{display:grid;place-items:center;width:44px;height:44px;border-radius:50%;background:#e8f3ee;color:#23765e;font-weight:700}
    .sender h2{margin:4px 0 2px;font-size:17px}.sender a{color:#52736a;font-size:13px;text-decoration:none}
    .status-pill{padding:7px 11px;border-radius:20px;font-size:12px;font-weight:700;white-space:nowrap}
    .pill-pending{background:#fff2d5;color:#97620c}.pill-approved{background:#e1f4eb;color:#217455}.pill-rejected{background:#fde8e6;color:#a9413c}
    .details-grid{display:grid;grid-template-columns:2fr 2fr 1fr;gap:16px;margin:22px 0;padding:16px 0;border-top:1px solid #edf1ef;border-bottom:1px solid #edf1ef}
    .details-grid strong{display:block;margin-top:7px;font-size:14px}
    .card-actions{display:flex;align-items:center;gap:8px}.action-spacer{flex:1}
    .reject-button{color:#a9413c}.decision-note{color:#217455;font-size:14px;font-weight:700}.rejected-note{color:#a9413c}
    .empty-state{padding:34px;border:1px dashed #ccd9d3;border-radius:12px;color:#65756f;text-align:center;background:#fbfdfc}
    @media(max-width:700px){.page-header{align-items:flex-start;flex-direction:column}.summary-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.details-grid{grid-template-columns:1fr 1fr}.card-top{align-items:flex-start;flex-direction:column}}
  `]
})
export class PharmacistRecipesComponent implements OnInit, OnDestroy {
  private refreshTimer?: number;
  private readonly api = inject(RecipesApiService);
  private readonly toaster = inject(ToasterService);
  items: Recipe[] = [];
  filter: 'All' | 'Pending' | 'Approved' | 'Rejected' = 'All';

  get filteredItems(): Recipe[] {
    return this.filter === 'All' ? this.items : this.items.filter(item => item.status === this.filter);
  }
  count(status: string): number { return this.items.filter(item => item.status === status).length; }
  initials(name?: string | null): string {
    const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);
    return parts.slice(0, 2).map(part => part[0]).join('').toUpperCase() || 'K';
  }
  statusLabel(status: string): string {
    return status === 'Approved' ? 'Odobren' : status === 'Rejected' ? 'Odbijen' : 'Ceka pregled';
  }

  ngOnInit(): void {
    this.load();
    this.refreshTimer = window.setInterval(() => this.load(false), 5000);
  }
  ngOnDestroy(): void {
    if (this.refreshTimer !== undefined) window.clearInterval(this.refreshTimer);
  }
  load(showError = true): void {
    this.api.getAll().subscribe({
      next: items => this.items = items,
      error: () => { if (showError) this.toaster.error('Recepte za pregled nije moguce ucitati.'); }
    });
  }
  setStatus(item: Recipe, status: string): void {
    this.api.updateStatus(item.id, status).subscribe({
      next: () => {
        item.status = status;
        this.toaster.success(status === 'Approved' ? 'Recept je odobren.' : 'Recept je odbijen.');
      },
      error: () => this.toaster.error('Nije moguce promijeniti status recepta.')
    });
  }
  download(item: Recipe): void {
    this.api.downloadScan(item.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = 'recept-' + item.id; a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toaster.error('Nije moguce preuzeti sken recepta.')
    });
  }
}