import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { StoreApiService, Product, ProductReview } from '../../../api-services/store/store-api.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({selector:'app-product-details',standalone:false,template:`
<section class="page detail-page" *ngIf="product as p; else loading">
  <a mat-button routerLink="/catalog">← Nazad na proizvode</a>
  <div class="detail-card"><div class="detail-image"><img [src]="p.imageUrl" [alt]="p.name" (error)="imageFallback($event)"></div>
    <div class="detail-info"><small>{{p.categoryName}}<ng-container *ngIf="p.brandName"> · {{p.brandName}}</ng-container></small><h1>{{p.name}}</h1>
      <p>{{p.description}}</p><p class="rating">★ {{p.averageRating | number:'1.1-1'}} <span>({{p.reviewCount}} recenzija)</span></p>
      <div class="price">{{p.currentPrice | number:'1.2-2'}} KM <del *ngIf="p.isDiscounted">{{p.price | number:'1.2-2'}} KM</del></div>
      <p>{{p.quantityInStock>0 ? 'Na stanju: '+p.quantityInStock : 'Trenutno nije dostupno'}}</p>
      <div class="detail-actions"><button mat-raised-button color="primary" (click)="addToCart()" [disabled]="p.quantityInStock<1">Dodaj u korpu</button><button mat-stroked-button (click)="favorite()">♡ Dodaj u favorite</button></div>
    </div></div>
  <section class="reviews"><h2>Ocjene i recenzije</h2><p *ngIf="!reviews.length">Još nema recenzija.</p>
    <article class="review" *ngFor="let r of reviews"><div class="review-heading"><strong>{{r.userName}}</strong><span>{{'★'.repeat(r.rating)}} <small>{{r.createdAtUtc | date:'mediumDate'}}</small></span></div><p>{{r.text}}</p></article>
    <form *ngIf="auth.isAuthenticated()" [formGroup]="reviewForm" (ngSubmit)="submitReview()"><mat-form-field appearance="outline"><mat-label>Ocjena</mat-label><mat-select formControlName="rating"><mat-option *ngFor="let n of [5,4,3,2,1]" [value]="n">{{n}} zvjezdica</mat-option></mat-select></mat-form-field><mat-form-field appearance="outline"><mat-label>Vaša recenzija</mat-label><textarea matInput rows="3" formControlName="text"></textarea></mat-form-field><button mat-raised-button color="primary" [disabled]="reviewForm.invalid">Pošalji recenziju</button></form>
  </section>
</section>
<ng-template #loading><section class="page">Učitavanje proizvoda...</section></ng-template>`,styles:[`
  .detail-page{max-width:1100px;margin:auto}.detail-card{display:grid;grid-template-columns:minmax(250px,1fr) 1.2fr;gap:2rem;margin:1rem 0 2rem;padding:2rem;border:1px solid #dce6df;border-radius:14px;background:#fff}.detail-image{display:grid;place-items:center;min-height:340px}.detail-image img{max-width:100%;max-height:420px;object-fit:contain}.detail-info h1{margin:.5rem 0 1rem}.detail-info p{line-height:1.7}.rating{color:#a36500;font-weight:700}.rating span{color:#536458;font-weight:400}.price{margin:1.5rem 0;color:#176b36;font-size:1.7rem;font-weight:700}.price del{margin-left:.75rem;color:#66766b;font-size:1rem;font-weight:400}.detail-actions{display:flex;flex-wrap:wrap;gap:.75rem}.reviews{padding:1.5rem;border:1px solid #dce6df;border-radius:14px;background:#fff}.review{padding:1rem 0;border-bottom:1px solid #e4ebe6}.review-heading{display:flex;justify-content:space-between;gap:1rem}.review-heading span{color:#a36500}.review-heading small{color:#526458}.reviews form{display:grid;grid-template-columns:180px 1fr auto;align-items:start;gap:1rem;margin-top:1.5rem}.reviews form mat-form-field{width:100%}@media(max-width:750px){.detail-card{grid-template-columns:1fr;padding:1rem}.detail-image{min-height:220px}.reviews form{grid-template-columns:1fr}.review-heading{flex-direction:column}}
` ]})
export class ProductDetailsComponent implements OnInit {
  private route=inject(ActivatedRoute); private api=inject(StoreApiService); private fb=inject(FormBuilder); private toaster=inject(ToasterService);
  auth=inject(AuthFacadeService); product?:Product; reviews:ProductReview[]=[];
  reviewForm=this.fb.group({rating:[5,[Validators.required]],text:['',[Validators.required,Validators.maxLength(2000)]]});
  ngOnInit(){const id=Number(this.route.snapshot.paramMap.get('id'));this.api.getProduct(id).subscribe({next:p=>{this.product=p;this.loadReviews();},error:()=>this.toaster.error('Proizvod nije pronađen.')});}
  loadReviews(){if(this.product)this.api.getReviews(this.product.id).subscribe(x=>this.reviews=x);}
  imageFallback(event:Event){(event.target as HTMLImageElement).src='/images/product-placeholder.svg';}
  addToCart(){if(!this.product)return;this.api.addToCart(this.product.id).subscribe({next:()=>this.toaster.success('Proizvod je dodan u korpu.'),error:()=>this.toaster.error('Prijavite se ili pokušajte ponovo.')});}
  favorite(){if(!this.product)return;this.api.addToWishlist(this.product.id).subscribe({next:()=>this.toaster.success('Dodano u favorite.'),error:()=>this.toaster.error('Prijavite se ili pokušajte ponovo.')});}
  submitReview(){if(!this.product||this.reviewForm.invalid)return;const v=this.reviewForm.getRawValue();this.api.addReview(this.product.id,v.rating!,v.text!).subscribe({next:()=>{this.reviewForm.reset({rating:5,text:''});this.loadReviews();this.toaster.success('Recenzija je sačuvana.');},error:()=>this.toaster.error('Recenziju nije moguće sačuvati.')});}
}
