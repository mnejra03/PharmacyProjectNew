import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({ selector: 'app-register', standalone: false, templateUrl: './register.component.html', styleUrl: './register.component.scss' })
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthFacadeService);
  private router = inject(Router);
  submitting = false;
  errorMessage = '';
  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.maxLength(30), Validators.pattern(/^(?:(?:060\d{7}|06[1-9]\d{6})|\+?387(?:60\d{7}|6[1-9]\d{6}))$/)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]]
  });
  get passwordStrength(): number {
    const value = this.form.controls.password.value ?? '';
    return [value.length >= 8, /[A-Z]/.test(value), /[a-z]/.test(value), /\d/.test(value), /[^A-Za-z0-9]/.test(value)].filter(Boolean).length;
  }
  get passwordStrengthLabel(): string { return ['Unesite lozinku', 'Vrlo slaba', 'Slaba', 'Srednja', 'Dobra', 'Jaka'][this.passwordStrength]; }
  submit(): void {
    if (this.form.invalid || this.submitting) { this.form.markAllAsTouched(); return; }
    this.submitting = true; this.errorMessage = '';
    this.auth.register({
      firstName: this.form.value.firstName ?? '', lastName: this.form.value.lastName ?? '',
      email: this.form.value.email ?? '', phoneNumber: this.form.value.phoneNumber,
      password: this.form.value.password ?? ''
    }).subscribe({
      next: () => { this.submitting = false; this.router.navigate(['/client']); },
      error: (error: any) => {
        this.submitting = false;
        const apiMessage = error?.error?.message ?? error?.error?.Message;
        this.errorMessage = error?.status === 409 && typeof apiMessage === 'string'
          ? apiMessage
          : error?.status === 409
            ? 'E-mail adresa je već povezana s postojećim ili ranije uklonjenim računom.'
            : 'Registracija nije uspjela. Provjerite podatke i pokušajte ponovo.';
      }
    });
  }
}
