import { Routes } from '@angular/router';
import { CheckoutComponent } from './components/checkout/checkout.component';
import { CartCheckoutComponent } from './components/cart-checkout/cart-checkout.component';

export const PAYMENT_ROUTES: Routes = [
  {
    path: 'checkout-cart',
    component: CartCheckoutComponent
  },
  {
    path: ':courseId',
    component: CheckoutComponent
  }
];
