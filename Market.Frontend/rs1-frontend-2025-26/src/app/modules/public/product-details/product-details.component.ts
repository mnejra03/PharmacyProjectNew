import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { StoreApiService, Product, ProductReview } from '../../../api-services/store/store-api.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({selector:'app-product-details',standalone:false,template:`
<section class="page detail-page" *ngIf="product as p; else loading">
  <a mat-button routerLink="/catalog">← Nazad na proizvode</a>
  <div class="detail-card"><div class="detail-image"><img [src]="p.imageUrl" [alt]="p.name" (error)="imageFallback($event)"><button class="zoom-image-button" mat-icon-button type="button" (click)="openImageZoom()" [attr.aria-label]="'Uvećaj sliku: ' + p.name"><mat-icon>zoom_in</mat-icon></button></div>
    <div class="detail-info"><small>{{p.categoryName}}<ng-container *ngIf="p.brandName"> · {{p.brandName}}</ng-container></small><h1>{{p.name}}</h1>
      <p>{{p.description}}</p><p class="rating">★ {{p.averageRating | number:'1.1-1'}} <span>({{p.reviewCount}} recenzija)</span></p>
      <div class="price">{{p.currentPrice | number:'1.2-2'}} KM <del *ngIf="p.isDiscounted">{{p.price | number:'1.2-2'}} KM</del></div>
      <p>{{p.quantityInStock>0 ? 'Na stanju: '+p.quantityInStock : 'Trenutno nije dostupno'}}</p>
      <div class="detail-actions"><button mat-raised-button color="primary" (click)="addToCart()" [disabled]="p.quantityInStock<1">Dodaj u korpu</button><button mat-stroked-button (click)="favorite()">♡ Dodaj u favorite</button></div>
    </div></div>
  <section class="reviews"><h2>Ocjene i recenzije</h2><p *ngIf="!reviews.length">Još nema recenzija.</p>
    <article class="review" *ngFor="let r of reviews"><div class="review-heading"><strong>{{r.userName}}</strong><span>{{'★'.repeat(r.rating)}} <small>{{r.createdAtUtc | date:'mediumDate'}}</small></span></div><p>{{r.text}}</p></article>
    <p class="review-policy" *ngIf="auth.isAuthenticated()">Dozvoljena je jedna recenzija po proizvodu.</p><form *ngIf="auth.isAuthenticated()" [formGroup]="reviewForm" (ngSubmit)="submitReview()"><mat-form-field appearance="outline"><mat-label>Ocjena</mat-label><mat-select formControlName="rating"><mat-option *ngFor="let n of [5,4,3,2,1]" [value]="n">{{n}} zvjezdica</mat-option></mat-select></mat-form-field><mat-form-field appearance="outline"><mat-label>Vaša recenzija</mat-label><textarea matInput rows="3" formControlName="text"></textarea></mat-form-field><button mat-raised-button color="primary" [disabled]="reviewForm.invalid">Pošalji recenziju</button></form>
  </section>
  <div class="image-zoom-backdrop" *ngIf="imageZoomOpen && product as p" (click)="closeImageZoom()" role="dialog" aria-modal="true" [attr.aria-label]="p.name + ' - uvećani prikaz slike'">
    <section class="image-zoom-dialog" (click)="$event.stopPropagation()">
      <div class="image-zoom-toolbar" aria-label="Kontrole zumiranja">
        <button mat-icon-button type="button" aria-label="Umanji sliku" (click)="zoomOut()" [disabled]="imageZoomLevel <= 1"><mat-icon>remove</mat-icon></button>
        <span>{{imageZoomLevel * 100}}%</span>
        <button mat-icon-button type="button" aria-label="Uvećaj sliku" (click)="zoomIn()" [disabled]="imageZoomLevel >= 3"><mat-icon>add</mat-icon></button>
        <button mat-button type="button" (click)="resetImageZoom()">Vrati na 100%</button>
        <button mat-icon-button type="button" aria-label="Zatvori uvećani prikaz" (click)="closeImageZoom()"><mat-icon>close</mat-icon></button>
      </div>
      <div class="image-zoom-viewport"><img [src]="p.imageUrl" [alt]="p.name" [style.transform]="'scale(' + imageZoomLevel + ')'" (error)="imageFallback($event)"></div>
    </section>
  </div>
</section>
<ng-template #loading><section class="page">Učitavanje proizvoda...</section></ng-template>`,styles:[`
  .detail-page{max-width:1100px;margin:auto}.detail-card{display:grid;grid-template-columns:minmax(250px,1fr) 1.2fr;gap:2rem;margin:1rem 0 2rem;padding:2rem;border:1px solid #dce6df;border-radius:14px;background:#fff}.detail-image{display:grid;place-items:center;min-height:340px}.detail-image img{max-width:100%;max-height:420px;object-fit:contain}.detail-info h1{margin:.5rem 0 1rem}.detail-info p{line-height:1.7}.rating{color:#a36500;font-weight:700}.rating span{color:#536458;font-weight:400}.price{margin:1.5rem 0;color:#176b36;font-size:1.7rem;font-weight:700}.price del{margin-left:.75rem;color:#66766b;font-size:1rem;font-weight:400}.detail-actions{display:flex;flex-wrap:wrap;gap:.75rem}.reviews{padding:1.5rem;border:1px solid #dce6df;border-radius:14px;background:#fff}.review{padding:1rem 0;border-bottom:1px solid #e4ebe6}.review-heading{display:flex;justify-content:space-between;gap:1rem}.review-heading span{color:#a36500}.review-heading small{color:#526458}.review-policy{margin:1rem 0 0;color:#65756f;font-size:.92rem}.reviews form{display:grid;grid-template-columns:180px 1fr auto;align-items:start;gap:1rem;margin-top:1.5rem}.reviews form mat-form-field{width:100%}@media(max-width:750px){.detail-card{grid-template-columns:1fr;padding:1rem}.detail-image{min-height:220px}.reviews form{grid-template-columns:1fr}.review-heading{flex-direction:column}}
  .detail-image{position:relative}.zoom-image-button{position:absolute;top:.75rem;right:.75rem;display:grid;place-items:center;width:44px;height:44px;border:1px solid rgba(23,62,39,.18);border-radius:50%;background:rgba(255,255,255,.94);color:#173e27;box-shadow:0 2px 10px rgba(0,0,0,.16)}.zoom-image-button:hover{background:#173e27;color:#fff}.image-zoom-backdrop{position:fixed;inset:0;z-index:2000;display:grid;place-items:center;padding:1rem;background:rgba(8,18,12,.88)}.image-zoom-dialog{width:min(1100px,96vw);max-height:96vh;padding:.75rem;border:1px solid rgba(255,255,255,.2);border-radius:14px;background:#fff;box-shadow:0 20px 70px rgba(0,0,0,.35)}.image-zoom-toolbar{display:flex;align-items:center;justify-content:center;gap:.5rem;padding-bottom:.5rem;color:#173e27}.image-zoom-toolbar span{min-width:3.5rem;text-align:center;font-weight:700}.image-zoom-toolbar button:last-child{margin-left:auto}.image-zoom-viewport{display:grid;place-items:center;max-height:calc(96vh - 80px);overflow:auto}.image-zoom-viewport img{max-width:85vw;max-height:78vh;object-fit:contain;transition:transform .2s ease;transform-origin:center}@media(max-width:750px){.image-zoom-toolbar{gap:.1rem}.image-zoom-toolbar button{min-width:40px;padding:0}.image-zoom-viewport img{max-width:82vw;max-height:72vh}}
` ]})
export class ProductDetailsComponent implements OnInit {
  private route=inject(ActivatedRoute); private api=inject(StoreApiService); private fb=inject(FormBuilder); private toaster=inject(ToasterService);
  auth=inject(AuthFacadeService); product?:Product; reviews:ProductReview[]=[];
  imageZoomOpen=false; imageZoomLevel=1;
  reviewForm=this.fb.group({rating:[5,[Validators.required]],text:['',[Validators.required,Validators.maxLength(2000)]]});
  ngOnInit(){const id=Number(this.route.snapshot.paramMap.get('id'));this.api.getProduct(id).subscribe({next:p=>{this.product=p;this.loadReviews();},error:()=>this.toaster.error('Proizvod nije pronađen.')});}
  loadReviews(){if(this.product)this.api.getReviews(this.product.id).subscribe(x=>this.reviews=x);}
  openImageZoom(){this.imageZoomLevel=1;this.imageZoomOpen=true;}
  closeImageZoom(){this.imageZoomOpen=false;}
  zoomIn(){this.imageZoomLevel=Math.min(3,Number((this.imageZoomLevel+.25).toFixed(2)));}
  zoomOut(){this.imageZoomLevel=Math.max(1,Number((this.imageZoomLevel-.25).toFixed(2)));}
  resetImageZoom(){this.imageZoomLevel=1;}
  imageFallback(event:Event){(event.target as HTMLImageElement).src='/images/product-placeholder.svg';}
  addToCart(){if(!this.product)return;this.api.addToCart(this.product.id).subscribe({next:()=>this.toaster.success('Proizvod je dodan u korpu.'),error:()=>this.toaster.error('Prijavite se ili pokušajte ponovo.')});}
  favorite(){if(!this.product)return;this.api.addToWishlist(this.product.id).subscribe({next:()=>this.toaster.success('Dodano u favorite.'),error:()=>this.toaster.error('Prijavite se ili pokušajte ponovo.')});}
  submitReview(){if(!this.product||this.reviewForm.invalid)return;const v=this.reviewForm.getRawValue();this.api.addReview(this.product.id,v.rating!,v.text!).subscribe({next:()=>{this.reviewForm.reset({rating:5,text:''});this.loadReviews();this.toaster.success('Recenzija je sačuvana.');},error:(err)=>this.toaster.error(err.status===409 ? 'Dozvoljena je jedna recenzija po proizvodu.' : 'Recenziju nije moguce sacuvati.')});}
}
