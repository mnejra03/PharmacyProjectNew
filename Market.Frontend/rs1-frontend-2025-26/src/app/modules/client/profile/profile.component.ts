import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { UsersApiService } from '../../../api-services/users/users-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-profile',
  standalone: false,
  template: `
    <section class="page profile-page">
      <h1>Moj profil</h1>
      <div class="profile-card">
        <img *ngIf="profileImageUrl" [src]="profileImageUrl" alt="Profilna slika" width="120" height="120">
        <label class="upload-label">Profilna slika (JPEG, PNG, WEBP ili GIF; do 5 MB)
          <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" (change)="uploadImage($event)">
        </label>
        <form [formGroup]="form" (ngSubmit)="save()">
          <mat-form-field appearance="outline"><mat-label>Ime</mat-label><input matInput formControlName="firstName"></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Prezime</mat-label><input matInput formControlName="lastName"></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Telefon</mat-label><input matInput formControlName="phoneNumber" placeholder="+387 61 123 456"></mat-form-field>
          <button mat-raised-button color="primary" [disabled]="form.invalid || saving">{{saving ? 'Čuvam...' : 'Sačuvaj profil'}}</button>
        </form>
      </div>
      <h2>Promjena lozinke</h2>
      <form class="password-form" [formGroup]="passwordForm" (ngSubmit)="changePassword()">
        <mat-form-field appearance="outline"><mat-label>Trenutna lozinka</mat-label><input matInput type="password" formControlName="currentPassword"></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Nova lozinka</mat-label><input matInput type="password" formControlName="newPassword"></mat-form-field>
        <button mat-raised-button color="accent" [disabled]="passwordForm.invalid || changingPassword">Promijeni lozinku</button>
      </form>
    </section>
  `,
  styles: [`
    .profile-page{max-width:850px;margin:0 auto;padding:1.5rem;color:#203629}.profile-page h1{color:#14532d;margin-bottom:1rem}.profile-page h2{margin:2rem 0 .75rem;color:#174ea6}.profile-card,.password-form{padding:1.25rem;background:#fff;border:1px solid #dce6df;border-radius:14px;box-shadow:0 5px 18px #173e2710}.profile-card>img{display:block;object-fit:cover;border-radius:50%;border:3px solid #a7f3d0;margin-bottom:1rem}.upload-label{display:inline-grid;gap:.5rem;padding:.8rem 1rem;margin-bottom:1rem;border:1px dashed #2e7d32;border-radius:9px;color:#17492a;font-weight:600}.profile-card form,.password-form{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.75rem 1rem}.profile-card form button,.password-form button{justify-self:start;align-self:center}.password-form{grid-template-columns:repeat(2,minmax(0,1fr))}@media(max-width:600px){.profile-card form,.password-form{grid-template-columns:1fr}.profile-page{padding:1rem .5rem}}
  `]
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(UsersApiService);
  private readonly toaster = inject(ToasterService);
  profileImageUrl?: string;
  saving = false;
  changingPassword = false;

  readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.maxLength(30)]]
  });
  readonly passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  ngOnInit(): void {
    this.api.getMe().subscribe({
      next: user => { this.form.patchValue(user); this.profileImageUrl = user.profileImageUrl; },
      error: () => this.toaster.error('Profil nije moguće učitati.')
    });
  }

  uploadImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024 || !file.type.startsWith('image/')) {
      this.toaster.error('Odaberite sliku do 5 MB u JPEG, PNG, WEBP ili GIF formatu.');
      input.value = '';
      return;
    }
    this.api.updateProfileImage(file).subscribe({
      next: user => { this.profileImageUrl = user.profileImageUrl; this.toaster.success('Profilna slika je sačuvana.'); input.value = ''; },
      error: () => this.toaster.error('Profilnu sliku nije moguće sačuvati.')
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    this.api.updateMe({
      firstName: this.form.value.firstName!,
      lastName: this.form.value.lastName!,
      phoneNumber: this.form.value.phoneNumber?.trim() || undefined
    }).subscribe({
      next: user => { this.form.patchValue(user); this.saving = false; this.toaster.success('Profil i broj telefona su sačuvani.'); },
      error: () => { this.saving = false; this.toaster.error('Profil nije moguće sačuvati.'); }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    this.changingPassword = true;
    this.api.changePassword({ currentPassword: this.passwordForm.value.currentPassword!, newPassword: this.passwordForm.value.newPassword! }).subscribe({
      next: () => { this.passwordForm.reset(); this.changingPassword = false; this.toaster.success('Lozinka je uspješno promijenjena.'); },
      error: (error: any) => { this.changingPassword = false; this.toaster.error(error?.error?.detail ?? 'Lozinku nije moguće promijeniti. Provjerite trenutnu lozinku.'); }
    });
  }
}
