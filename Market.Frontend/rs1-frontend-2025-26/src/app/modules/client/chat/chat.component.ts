import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ChatApiService, ChatContact, ChatMessage } from '../../../api-services/chat/chat-api.service';
import { ChatRealtimeService } from '../../../core/services/chat-realtime.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({selector:'app-chat',standalone:false,template:`<section class="page"><h1>Razgovor s farmaceutom</h1>
  <mat-form-field *ngIf="contacts.length"><mat-label>{{auth.isPharmacist()?'Korisnik':'Farmaceut'}}</mat-label><mat-select [value]="selectedId" (selectionChange)="selectContact($event.value)"><mat-option *ngFor="let c of contacts" [value]="c.id">{{c.firstName}} {{c.lastName}} · {{c.email}}</mat-option></mat-select></mat-form-field>
  <p *ngIf="!contacts.length">Nema dostupnog kontakta za razgovor.</p><mat-list><mat-list-item *ngFor="let item of messages"><b>{{item.isResponse?'Farmaceut':'Korisnik'}}:</b>&nbsp;{{item.message}} <small>{{item.sentAtUtc | date:'short'}}</small></mat-list-item></mat-list>
  <form [formGroup]="form" (ngSubmit)="send()"><mat-form-field><mat-label>Poruka</mat-label><textarea matInput formControlName="message" rows="3"></textarea></mat-form-field><button mat-raised-button color="primary" [disabled]="form.invalid || !selectedId">Pošalji</button></form>
</section>`})
export class ChatComponent implements OnInit, OnDestroy {
  private fb=inject(FormBuilder); private api=inject(ChatApiService); private realtime=inject(ChatRealtimeService); auth=inject(AuthFacadeService);
  contacts:ChatContact[]=[]; messages:ChatMessage[]=[]; selectedId?:number;
  form=this.fb.group({message:['',[Validators.required,Validators.maxLength(4000)]]}); private subscription?:Subscription;
  ngOnInit(){this.subscription=this.realtime.messages.subscribe(message=>{if(message.senderId===this.selectedId||message.receiverId===this.selectedId)this.messages=[...this.messages,message];});this.api.getContacts().subscribe(items=>{this.contacts=items;if(items.length)this.selectContact(items[0].id);});}
  ngOnDestroy(){this.subscription?.unsubscribe();}
  selectContact(id:number){this.selectedId=id;this.api.getMessages(id).subscribe(items=>this.messages=items);}
  send(){if(this.form.invalid||!this.selectedId)return;this.api.send({receiverId:this.selectedId,message:this.form.value.message!}).subscribe(item=>{this.messages=[...this.messages,item];this.form.reset();});}
}
