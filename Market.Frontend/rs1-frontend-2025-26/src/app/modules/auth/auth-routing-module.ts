import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {AuthLayoutComponent} from './auth-layout/auth-layout.component';
import {LoginComponent} from './login/login.component';
import {ForgotPasswordComponent} from './forgot-password/forgot-password.component';
import {RegisterComponent} from './register/register.component';
import {LogoutComponent} from './logout/logout.component';
import {ResetPasswordComponent} from './reset-password/reset-password.component';
import { guestOnlyGuard } from '../../core/guards/guest-only.guard';
import { confirmLogoutGuard } from '../../core/guards/confirm-logout.guard';

const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginComponent, canActivate: [guestOnlyGuard] },
       { path: 'register', component: RegisterComponent, canActivate: [guestOnlyGuard] },
       { path: 'forgot-password', component: ForgotPasswordComponent },
       { path: 'reset-password', component: ResetPasswordComponent },
      { path: 'logout', component: LogoutComponent, canActivate: [confirmLogoutGuard] },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthRoutingModule { }
