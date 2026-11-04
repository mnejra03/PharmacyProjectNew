import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';

@Component({
  selector: 'app-forgot-password',
  standalone: false,
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AuthApiService);
  readonly form = this.fb.group({ email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]] });
  submitting = false;
  submitted = false;

  submit(): void {
    if (this.form.invalid || this.submitting) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.api.requestPasswordReset(this.form.controls.email.value ?? '').subscribe({
      next: () => { this.submitting = false; this.submitted = true; },
      error: () => { this.submitting = false; this.submitted = true; }
    });
  }
}
