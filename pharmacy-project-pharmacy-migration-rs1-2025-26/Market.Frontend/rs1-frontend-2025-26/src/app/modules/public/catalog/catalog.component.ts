import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StoreApiService, Product, ProductCategory, ProductBrand } from '../../../api-services/store/store-api.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({selector:'app-catalog',standalone:false,template:`
<section class="page catalog-page">
  <div class="catalog-heading"><div><h1>Proizvodi apoteke</h1><p>Pronađite proizvode po kategoriji, brendu ili nazivu.</p></div><a mat-stroked-button routerLink="/">Početna</a></div>
  <div class="catalog-filters">
    <mat-form-field appearance="outline"><mat-label>Pretraži proizvode</mat-label><input matInput [(ngModel)]="search" (ngModelChange)="load()" placeholder="Naziv ili opis"></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Kategorija</mat-label><mat-select [(ngModel)]="categoryId" (selectionChange)="load()"><mat-option [value]="undefined">Sve kategorije</mat-option><mat-option *ngFor="let c of categories" [value]="c.id">{{c.name}}</mat-option></mat-select></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Brend</mat-label><mat-select [(ngModel)]="brandId" (selectionChange)="load()"><mat-option [value]="undefined">Svi brendovi</mat-option><mat-option *ngFor="let b of brands" [value]="b.id">{{b.name}}</mat-option></mat-select></mat-form-field>
    <mat-checkbox [(ngModel)]="discounted" (change)="load()">Samo sniženi</mat-checkbox>
  </div>
  <p class="result-count">Pronađeno proizvoda: {{total}}</p>
  <div class="product-grid" *ngIf="products.length; else empty">
    <mat-card class="product-card" *ngFor="let p of products">
      <a class="product-image" [routerLink]="['/product',p.id]"><img [src]="p.imageUrl" [alt]="p.name" loading="lazy" (error)="imageFallback($event)"></a>
      <mat-card-content><small>{{p.categoryName}}<ng-container *ngIf="p.brandName"> · {{p.brandName}}</ng-container></small><a class="product-name" [routerLink]="['/product',p.id]">{{p.name}}</a><p class="stock">{{p.quantityInStock > 0 ? 'Dostupno: ' + p.quantityInStock : 'Trenutno nije dostupno'}}</p><div class="price-row"><span [class.discount-price]="p.isDiscounted">{{p.currentPrice | number:'1.2-2'}} KM</span><del *ngIf="p.isDiscounted">{{p.price | number:'1.2-2'}} KM</del></div></mat-card-content>
      <mat-card-actions><button mat-raised-button color="primary" (click)="add(p)" [disabled]="p.quantityInStock<1">Dodaj u korpu</button><button mat-icon-button aria-label="Dodaj u favorite" (click)="favorite(p)"><mat-icon>favorite_border</mat-icon></button></mat-card-actions>
    </mat-card>
  </div>
  <ng-template #empty><div class="empty-state">Nema proizvoda za odabrane kriterije.</div></ng-template>
  <div class="catalog-pagination"><button mat-button (click)="changePage(-1)" [disabled]="page<=1">Prethodna</button><span>Stranica {{page}} od {{totalPages}}</span><button mat-button (click)="changePage(1)" [disabled]="page>=totalPages">Sljedeća</button></div>
</section>`,styles:[`
  .catalog-page { max-width: 1440px; margin: auto; }
  .catalog-heading { display:flex; align-items:center; justify-content:space-between; gap:1rem; margin-bottom:1.25rem; }
  .catalog-heading h1 { margin-bottom:.25rem; } .catalog-heading p { margin:0; }
  .catalog-filters { display:grid; grid-template-columns:2fr 1fr 1fr auto; align-items:center; gap:1rem; padding:1rem; border:1px solid #dce6df; border-radius:12px; background:#fff; }
  .catalog-filters mat-form-field { width:100%; margin:0; } .result-count { color:#43584a; }
  .product-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(220px,260px)); justify-content:center; gap:1.25rem; }
  .product-card { display:flex; flex-direction:column; min-width:0; overflow:hidden; padding:0; border:1px solid #dce6df; border-radius:12px; background:#fff; color:#172b24; }
  .product-image { display:grid; place-items:center; height:210px; padding:1rem; background:#fff; }
  .product-image img { max-width:100%; max-height:100%; object-fit:contain; }
  .product-card mat-card-content { display:flex; flex:1; flex-direction:column; padding:1rem; }
  .product-card small { color:#526458; } .product-name { display:block; margin:.5rem 0; color:#173e27; font-size:1.05rem; font-weight:700; text-decoration:none; }
  .stock { margin:auto 0 .75rem; font-size:.9rem; } .price-row { display:flex; align-items:center; gap:.75rem; font-size:1.15rem; font-weight:700; }
  .price-row del { color:#66766b; font-size:.95rem; font-weight:400; } .discount-price { color:#a31d24; }
  .product-card mat-card-actions { display:flex; justify-content:space-between; padding:.5rem 1rem 1rem; }
  .empty-state { padding:3rem; border-radius:12px; background:#fff; color:#34483b; text-align:center; }
  .catalog-pagination { display:flex; justify-content:center; align-items:center; gap:1rem; margin-top:1.5rem; }
  @media(max-width:850px){.catalog-filters{grid-template-columns:1fr 1fr}.catalog-filters mat-checkbox{grid-column:span 2}}
  @media(max-width:560px){.catalog-heading{align-items:flex-start}.catalog-filters{grid-template-columns:1fr}.catalog-filters mat-checkbox{grid-column:auto}.product-grid{grid-template-columns:1fr 1fr;gap:.75rem}.product-image{height:150px}.product-card mat-card-actions button{font-size:.78rem;padding:0 .5rem}}
` ]})
export class CatalogComponent implements OnInit {
  private api=inject(StoreApiService); private auth=inject(AuthFacadeService); private toaster=inject(ToasterService); private route=inject(ActivatedRoute);
  products:Product[]=[]; categories:ProductCategory[]=[]; brands:ProductBrand[]=[]; search=''; categoryId?:number; brandId?:number; discounted=false; page=1; total=0; totalPages=1;
  ngOnInit(){
    this.api.getCategories().subscribe(x=>{this.categories=x;this.applyQuery(this.route.snapshot.queryParamMap.get('category'),this.route.snapshot.queryParamMap.get('brandId'));});
    this.api.getBrands().subscribe(x=>{this.brands=x;this.applyQuery(this.route.snapshot.queryParamMap.get('category'),this.route.snapshot.queryParamMap.get('brandId'));});
    this.route.queryParamMap.subscribe(params=>this.applyQuery(params.get('category'),params.get('brandId')));
  }
  private applyQuery(categoryName:string|null,brandValue:string|null){const categoryId=categoryName?this.categories.find(c=>c.name===categoryName)?.id:undefined;const brandId=brandValue?Number(brandValue):undefined;if(categoryId!==this.categoryId||brandId!==this.brandId){this.categoryId=categoryId;this.brandId=brandId;this.load();}else if(!this.products.length){this.load();}}
  load(){this.page=1;this.fetch();}
  fetch(){this.api.getProducts({search:this.search||undefined,categoryId:this.categoryId,brandId:this.brandId,discounted:this.discounted||undefined,page:this.page,pageSize:12}).subscribe(x=>{this.products=x.items;this.total=x.totalItems;this.totalPages=Math.max(1,x.totalPages);});}
  changePage(delta:number){this.page+=delta;this.fetch();window.scrollTo({top:0,behavior:'smooth'});}
  imageFallback(event:Event){(event.target as HTMLImageElement).src='/images/product-placeholder.svg';}
  add(p:Product){if(!this.auth.isAuthenticated()){this.toaster.error('Prijavite se da biste dodali proizvod u korpu.');return;}this.api.addToCart(p.id).subscribe({next:()=>this.toaster.success('Proizvod je dodan u korpu.'),error:()=>this.toaster.error('Proizvod nije moguće dodati u korpu.')});}
  favorite(p:Product){if(!this.auth.isAuthenticated()){this.toaster.error('Prijavite se da biste sačuvali favorite.');return;}this.api.addToWishlist(p.id).subscribe({next:()=>this.toaster.success('Dodano u favorite.'),error:()=>this.toaster.error('Proizvod nije moguće dodati u favorite.')});}
}
