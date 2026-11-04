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
    phoneNumber: ['', [Validators.maxLength(30)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]]
  });
  submit(): void {
    if (this.form.invalid || this.submitting) { this.form.markAllAsTouched(); return; }
    this.submitting = true; this.errorMessage = '';
    this.auth.register({
      firstName: this.form.value.firstName ?? '', lastName: this.form.value.lastName ?? '',
      email: this.form.value.email ?? '', phoneNumber: this.form.value.phoneNumber,
      password: this.form.value.password ?? ''
    }).subscribe({ next: () => { this.submitting = false; this.router.navigate(['/client']); }, error: () => { this.submitting = false; this.errorMessage = 'Registracija nije uspjela. Provjerite podatke i pokušajte ponovo.'; } });
  }
}
