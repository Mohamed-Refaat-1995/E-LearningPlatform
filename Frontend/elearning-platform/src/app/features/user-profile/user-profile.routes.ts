import { Routes } from '@angular/router';
import { ProfileComponent } from './components/profile/profile.component';
import { SecuritySettingsComponent } from './components/security-settings/security-settings.component';

export const USER_PROFILE_ROUTES: Routes = [
  {
    path: '',
    component: ProfileComponent
  },
  {
    path: 'security',
    component: SecuritySettingsComponent
  }
];
