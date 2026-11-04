import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  return control.get('newPassword')?.value === control.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-reset-password',
  standalone: false,
  template: `
    <section class="reset-card">
      <h1>Postavi novu lozinku</h1>
      <p *ngIf="!token" class="error-message">Link nema token. Zatraži novi link za promjenu lozinke.</p>
      <p *ngIf="errorMessage" class="error-message">{{errorMessage}}</p>
      <form *ngIf="token && !complete" [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline"><mat-label>Nova lozinka</mat-label><input matInput type="password" formControlName="newPassword" autocomplete="new-password">
          <mat-error *ngIf="form.controls.newPassword.hasError('minlength')">Lozinka mora imati najmanje 8 znakova.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Potvrdi novu lozinku</mat-label><input matInput type="password" formControlName="confirmPassword" autocomplete="new-password"></mat-form-field>
        <p *ngIf="form.hasError('passwordMismatch') && form.controls.confirmPassword.touched" class="error-message">Lozinke se ne podudaraju.</p>
        <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || submitting">{{submitting?'Spremam…':'Promijeni lozinku'}}</button>
      </form>
      <p *ngIf="complete" class="success-message" role="status">Lozinka je promijenjena. Sada se možeš prijaviti.</p>
      <a routerLink="/auth/login">Nazad na prijavu</a>
    </section>
  `,
  styles: [`.reset-card{width:min(100% - 2rem,440px);margin:4rem auto;padding:2rem;border-radius:16px;background:#fff;box-shadow:0 12px 36px rgba(16,60,32,.14);color:#24352a}.reset-card h1{margin:0 0 .75rem;color:#174d2b}.reset-card form{display:grid;gap:.75rem;margin:1.25rem 0}.reset-card mat-form-field{width:100%}.reset-card button{min-height:48px}.reset-card a{color:#176b36;font-weight:700}.error-message{padding:.8rem;border-radius:8px;background:#fee2e2;color:#991b1b}.success-message{padding:1rem;border-radius:10px;background:#e8f5e9;color:#174d2b}`]
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AuthApiService);
  private readonly route = inject(ActivatedRoute);
  readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';
  readonly form = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordsMatch });
  submitting = false;
  complete = false;
  errorMessage = '';

  submit(): void {
    if (this.form.invalid || !this.token || this.submitting) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.errorMessage = '';
    this.api.resetPassword(this.token, this.form.controls.newPassword.value ?? '').subscribe({
      next: () => { this.submitting = false; this.complete = true; },
      error: () => { this.submitting = false; this.errorMessage = 'Link za promjenu lozinke je nevažeći ili istekao. Zatraži novi link.'; }
    });
  }
}
