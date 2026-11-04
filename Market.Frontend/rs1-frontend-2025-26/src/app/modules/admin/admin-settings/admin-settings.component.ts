import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { UsersApiService } from '../../../api-services/users/users-api.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-admin-settings',
  standalone: false,
  templateUrl: './admin-settings.component.html',
  styleUrl: './admin-settings.component.scss',
})
export class AdminSettingsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(UsersApiService);
  private readonly toaster = inject(ToasterService);
  savingProfile = false;
  changingPassword = false;
  profileImageUrl?: string;

  profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.maxLength(30)]]
  });
  passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  ngOnInit(): void {
    this.api.getMe().subscribe({
      next: profile => {
        this.profileImageUrl = profile.profileImageUrl;
        this.profileForm.patchValue({ firstName: profile.firstName, lastName: profile.lastName, phoneNumber: profile.phoneNumber ?? '' });
      },
      error: () => this.toaster.error('Podatke administratorskog profila nije moguće učitati.')
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.savingProfile = true;
    const profile = this.profileForm.getRawValue();
    this.api.updateMe({ firstName: profile.firstName ?? '', lastName: profile.lastName ?? '', phoneNumber: profile.phoneNumber ?? '' }).subscribe({
      next: () => { this.savingProfile = false; this.toaster.success('Postavke profila su sačuvane.'); },
      error: () => { this.savingProfile = false; this.toaster.error('Postavke profila nije moguće sačuvati.'); }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    this.changingPassword = true;
    const passwords = this.passwordForm.getRawValue();
    this.api.changePassword({ currentPassword: passwords.currentPassword ?? '', newPassword: passwords.newPassword ?? '' }).subscribe({
      next: () => { this.changingPassword = false; this.passwordForm.reset(); this.toaster.success('Lozinka je promijenjena.'); },
      error: () => { this.changingPassword = false; this.toaster.error('Lozinku nije moguće promijeniti. Provjerite trenutnu lozinku.'); }
    });
  }

  uploadProfileImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (file.size > 5 * 1024 * 1024 || !['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type)) {
      this.toaster.error('Odaberite JPEG, PNG, WebP ili GIF sliku do 5 MB.'); input.value = ''; return;
    }
    this.api.updateProfileImage(file).subscribe({
      next: profile => { this.profileImageUrl = profile.profileImageUrl; this.toaster.success('Profilna slika je sačuvana u Azure Blob Storage.'); input.value = ''; },
      error: () => { this.toaster.error('Slika nije mogla biti otpremljena u Azure Blob Storage.'); input.value = ''; }
    });
  }
}
