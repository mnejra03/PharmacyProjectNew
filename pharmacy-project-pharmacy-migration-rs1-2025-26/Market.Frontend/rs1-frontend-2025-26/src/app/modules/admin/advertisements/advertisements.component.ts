import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AdvertisementsApiService, Advertisement } from '../../../api-services/advertisements/advertisements-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-advertisements',
  standalone: false,
  template: `
    <section class="page">
      <h1>Oglasi i slike</h1>
      <form [formGroup]="form" (ngSubmit)="save()">
        <mat-form-field appearance="outline"><mat-label>Naslov</mat-label><input matInput formControlName="title"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>URL slike</mat-label><input matInput formControlName="imageUrl" type="url"></mat-form-field>
        <label class="ad-image-drop" [class.drag-over]="dragOver" (dragover)="onDragOver($event)" (dragleave)="onDragLeave($event)" (drop)="onDrop($event)">
          <span>Prevuci sliku ovdje ili klikni za odabir (JPEG, PNG, WEBP ili GIF do 5 MB).</span>
          <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" [disabled]="saving" (change)="pick($event)">
          <small *ngIf="image">Odabrano: {{image.name}}</small>
        </label>
        <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || saving">{{saving?'Spremam…':selected?'Sačuvaj':'Dodaj oglas'}}</button>
        <button mat-button type="button" *ngIf="selected" (click)="reset()">Odustani</button>
      </form>
      <mat-list><mat-list-item *ngFor="let item of items"><img matListItemAvatar [src]="item.imageUrl" [alt]="item.title"><span matListItemTitle>{{item.title}}</span><button mat-button (click)="edit(item)">Uredi</button><button mat-button color="warn" (click)="remove(item.id)">Obriši</button></mat-list-item></mat-list>
    </section>
  `,
  styles: [`.ad-image-drop{display:grid;gap:.5rem;padding:1rem;border:1px dashed #95aa9a;border-radius:8px;cursor:pointer;transition:background .15s,border-color .15s}.ad-image-drop.drag-over{background:#e8f5e9;border-color:#2e7d32;box-shadow:0 0 0 3px rgba(46,125,50,.14)}`]
})
export class AdvertisementsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AdvertisementsApiService);
  private readonly toaster = inject(ToasterService);
  items: Advertisement[] = [];
  selected?: Advertisement;
  image?: File;
  dragOver = false;
  saving = false;
  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    imageUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/.+/i)]]
  });

  ngOnInit(): void { this.load(); }
  load(): void { this.api.getAll().subscribe({ next: items => this.items = items, error: () => this.toaster.error('Oglase nije moguće učitati.') }); }

  pick(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.selectImage(file);
    (event.target as HTMLInputElement).value = '';
  }

  onDragOver(event: DragEvent): void { event.preventDefault(); this.dragOver = true; }
  onDragLeave(event: DragEvent): void { event.preventDefault(); this.dragOver = false; }
  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.selectImage(file);
  }

  edit(item: Advertisement): void { this.selected = item; this.image = undefined; this.form.patchValue(item); }
  reset(): void { this.selected = undefined; this.image = undefined; this.dragOver = false; this.form.reset(); }

  save(): void {
    if (this.form.invalid || (!this.image && !this.form.value.imageUrl) || this.saving) return;
    this.saving = true;
    if (this.image) {
      this.api.uploadImage(this.image).subscribe({
        next: result => this.persist(result.relativeUrl),
        error: () => { this.saving = false; this.toaster.error('Sliku oglasa nije moguće poslati.'); }
      });
    } else {
      this.persist(this.form.value.imageUrl!);
    }
  }

  remove(id: number): void {
    if (confirm('Obrisati oglas?')) this.api.delete(id).subscribe({ next: () => { this.load(); this.toaster.success('Oglas je obrisan.'); }, error: () => this.toaster.error('Oglas nije moguće obrisati.') });
  }

  private selectImage(file: File): void {
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
    if (file.size > 5 * 1024 * 1024 || !allowedTypes.includes(file.type)) {
      this.toaster.error('Odaberi JPEG, PNG, WEBP ili GIF sliku do 5 MB.');
      return;
    }
    this.image = file;
    this.toaster.info('Slika je odabrana i bit će poslana pri spremanju oglasa.');
  }

  private persist(imageUrl: string): void {
    const body = { title: this.form.value.title!, imageUrl };
    const request = this.selected ? this.api.update(this.selected.id, body) : this.api.create(body);
    request.subscribe({
      next: () => { this.saving = false; this.reset(); this.load(); this.toaster.success('Oglas je sačuvan.'); },
      error: () => { this.saving = false; this.toaster.error('Oglas nije moguće sačuvati.'); }
    });
  }
}
