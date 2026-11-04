import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { RecipesApiService, Recipe } from '../../../api-services/recipes/recipes-api.service';
import { ToasterService } from '../../../core/services/toaster.service';
@Component({selector:'app-recipes',standalone:false,template:`<section class="page"><h1>Moji recepti</h1><form [formGroup]="form" (ngSubmit)="submit()"><mat-form-field><mat-label>Ime doktora</mat-label><input matInput formControlName="first"></mat-form-field><mat-form-field><mat-label>Prezime doktora</mat-label><input matInput formControlName="last"></mat-form-field><label>Sken recepta <input type="file" accept="image/*,.pdf" (change)="pick($event)"></label><button mat-raised-button color="primary" [disabled]="form.invalid">Pošalji recept</button></form><mat-list><mat-list-item *ngFor="let item of items">Dr. {{item.doctorFirstName}} {{item.doctorLastName}} — {{item.status}} <button mat-button *ngIf="item.hasScan" (click)="download(item)">Preuzmi sken</button></mat-list-item></mat-list></section>`})
export class RecipesComponent implements OnInit {
  private fb=inject(FormBuilder); api=inject(RecipesApiService); private toaster=inject(ToasterService); items:Recipe[]=[]; file?:File;
  form=this.fb.group({first:['',[Validators.required]],last:['',[Validators.required]]});
  ngOnInit(){this.load();} pick(e:Event){this.file=(e.target as HTMLInputElement).files?.[0];}
  load(){this.api.getMine().subscribe({next:x=>this.items=x,error:()=>this.toaster.error('Recepte nije moguće učitati.')});}
  submit(){if(this.form.invalid)return;this.api.create(this.form.value.first!,this.form.value.last!,this.file).subscribe({next:()=>{this.form.reset();this.file=undefined;this.load();this.toaster.success('Recept je poslan farmaceutu na pregled.');},error:()=>this.toaster.error('Recept nije moguće poslati.')});}
  download(item:Recipe){this.api.downloadScan(item.id).subscribe({next:blob=>{const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=`recipe-${item.id}`;a.click();URL.revokeObjectURL(url);this.toaster.success('Sken recepta je preuzet.');},error:()=>this.toaster.error('Sken recepta nije moguće preuzeti.')});}
}
