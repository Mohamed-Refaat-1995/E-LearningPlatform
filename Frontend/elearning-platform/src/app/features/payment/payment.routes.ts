import { Routes } from '@angular/router';
import { CheckoutComponent } from './components/checkout/checkout.component';

export const PAYMENT_ROUTES: Routes = [
  {
    path: ':courseId',
    component: CheckoutComponent
  }
];
