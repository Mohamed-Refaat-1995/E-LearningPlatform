import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },
      { path: 'forgot-password', component: ForgotPasswordComponent },
      { path: 'verify-email', component: VerifyEmailComponent },
      { path: 'reset-password', component: ResetPasswordComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  }
];

import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { VerifyEmailComponent } from './components/verify-email/verify-email.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
