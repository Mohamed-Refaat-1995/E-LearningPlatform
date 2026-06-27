import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '@shared/models/user.model';

export const rootRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.getToken() || !auth.isTokenValid()) {
    return router.createUrlTree(['/auth/login']);
  }

  const roleRoutes: Partial<Record<UserRole, string>> = {
    [UserRole.Instructor]: '/instructor',
    [UserRole.Admin]: '/admin',
    [UserRole.Student]: '/dashboard',
  };

  const user = auth.getCurrentUserSnapshot();
  const destination = (user && roleRoutes[user.role]) ?? '/auth/login';
  return router.createUrlTree([destination]);
};
