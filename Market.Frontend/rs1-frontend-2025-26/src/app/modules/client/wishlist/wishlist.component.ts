import { Component, OnInit, inject } from '@angular/core';
import { StoreApiService, Product } from '../../../api-services/store/store-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({selector:'app-wishlist',standalone:false,template:`<section class="page"><h1>Omiljeni proizvodi</h1><div class="wishlist-grid"><mat-card *ngFor="let p of products"><a [routerLink]="['/product',p.id]"><img [src]="p.imageUrl" [alt]="p.name"><strong>{{p.name}}</strong></a><p>{{p.currentPrice | number:'1.2-2'}} KM</p><button mat-button color="warn" (click)="remove(p)">Ukloni iz favorita</button></mat-card></div><p *ngIf="!products.length">Još nemate omiljenih proizvoda. <a routerLink="/catalog">Pregledaj katalog</a></p></section>`,styles:[`.wishlist-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:1rem}.wishlist-grid mat-card{padding:1rem}.wishlist-grid a{display:grid;gap:.75rem;color:#173e27;text-decoration:none}.wishlist-grid img{width:100%;height:170px;object-fit:contain}.wishlist-grid p{color:#176b36;font-weight:700}`]})
export class WishlistComponent implements OnInit {
  private api=inject(StoreApiService);private toaster=inject(ToasterService);products:Product[]=[];
  ngOnInit(){this.load();}load(){this.api.getWishlist().subscribe({next:x=>this.products=x,error:()=>this.toaster.error('Favorite nije moguće učitati.')});}
  remove(p:Product){this.api.removeFromWishlist(p.id).subscribe({next:()=>this.load(),error:()=>this.toaster.error('Proizvod nije moguće ukloniti.')});}
}
