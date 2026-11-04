import { Component, OnInit, inject } from '@angular/core';
import { RecipesApiService, Recipe } from '../../../api-services/recipes/recipes-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({selector:'app-pharmacist-recipes',standalone:false,template:`<section class="page"><h1>Recepti za pregled</h1><p *ngIf="!items.length">Nema recepata za pregled.</p><mat-card *ngFor="let item of items" class="recipe-card"><mat-card-header><mat-card-title>Recept #{{item.id}}</mat-card-title><mat-card-subtitle>Dr. {{item.doctorFirstName}} {{item.doctorLastName}} · {{item.dateOfIssue | date:'short'}}</mat-card-subtitle></mat-card-header><mat-card-content>Status: {{item.status}}<button mat-button *ngIf="item.hasScan" (click)="download(item)">Preuzmi sken</button></mat-card-content><mat-card-actions><button mat-button (click)="setStatus(item,'Approved')">Odobri</button><button mat-button color="warn" (click)="setStatus(item,'Rejected')">Odbij</button></mat-card-actions></mat-card></section>`})
export class PharmacistRecipesComponent implements OnInit {
  private readonly api=inject(RecipesApiService);
  private readonly toaster=inject(ToasterService);
  items:Recipe[]=[];
  ngOnInit(){this.load();}
  load(){this.api.getAll().subscribe({next:x=>this.items=x,error:()=>this.toaster.error('Recepte za pregled nije moguće učitati.')});}
  setStatus(item:Recipe,status:string){this.api.updateStatus(item.id,status).subscribe({next:()=>{item.status=status;this.toaster.success(status==='Approved'?'Recept je odobren.':'Recept je odbijen.');},error:()=>this.toaster.error('Status recepta nije moguće promijeniti.')});}
  download(item:Recipe){this.api.downloadScan(item.id).subscribe({next:blob=>{const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=`recipe-${item.id}`;a.click();URL.revokeObjectURL(url);this.toaster.success('Sken recepta je preuzet.');},error:()=>this.toaster.error('Sken recepta nije moguće preuzeti.')});}
}
