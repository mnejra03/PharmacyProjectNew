import { Component, OnInit, inject } from '@angular/core';
import { DashboardApiService, DashboardStats } from '../../../api-services/dashboard/dashboard-api.service';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  template: `<section class="page dashboard-page" aria-labelledby="dashboard-title">
    <h1 id="dashboard-title">Pregled apoteke</h1>
    <div class="dashboard-cards" *ngIf="stats">
      <article class="card"><h2>Korisnici</h2><p>{{ stats.users }}</p></article>
      <article class="card"><h2>Farmaceuti</h2><p>{{ stats.pharmacists }}</p></article>
      <article class="card"><h2>Recepti</h2><p>{{ stats.recipes }}</p></article>
      <article class="card"><h2>Recepti na čekanju</h2><p>{{ stats.pendingRecipes }}</p></article>
      <article class="card"><h2>Nepročitane obavijesti</h2><p>{{ stats.unreadNotifications }}</p></article>
    </div>
  </section>`,
  styles: [`
    :host { display:block; color:#333; font-family:Poppins,Arial,sans-serif; }
    .dashboard-page { min-height:80vh; padding:1.5rem 2rem; }
    h1 { margin:0 0 1rem; color:#1b5e20; font-size:1.7rem; }
    .dashboard-cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(220px,1fr)); gap:1.5rem; padding:1rem 0; }
    .card { min-height:125px; padding:1.5rem; border-radius:10px; background:#fff; box-shadow:0 2px 10px rgba(0,0,0,.08); text-align:center; }
    .card h2 { margin:0 0 .75rem; color:#59605a; font-size:1rem; font-weight:600; }
    .card p { margin:0; color:#1b5e20; font-size:2rem; font-weight:700; }
    @media(max-width:600px){.dashboard-page{padding:1rem}.dashboard-cards{grid-template-columns:1fr;gap:.875rem}}
  `],
})
export class DashboardComponent implements OnInit {
  private api = inject(DashboardApiService);
  stats?: DashboardStats;

  ngOnInit(): void {
    this.api.getStats().subscribe((stats) => (this.stats = stats));
  }
}
