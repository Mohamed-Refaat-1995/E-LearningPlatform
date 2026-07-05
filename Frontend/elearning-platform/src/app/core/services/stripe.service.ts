import { Injectable } from '@angular/core';
import { loadStripe, Stripe, StripeCardElement, StripeElements } from '@stripe/stripe-js';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StripeService {
  private stripePromise: Promise<Stripe | null> | null = null;
  private elements: StripeElements | null = null;
  private cardElement: StripeCardElement | null = null;

  private getStripe(): Promise<Stripe | null> {
    if (!this.stripePromise) {
      this.stripePromise = loadStripe(environment.stripePublishableKey);
    }
    return this.stripePromise;
  }

  /** Mounts a Stripe Card Element into the DOM element with the given id. Call once the container exists (e.g. after the client secret is known). */
  async mountCardElement(containerId: string): Promise<StripeCardElement> {
    const stripe = await this.getStripe();
    if (!stripe) {
      throw new Error('Stripe failed to load. Check your internet connection and try again.');
    }

    this.unmountCardElement();
    this.elements = stripe.elements();
    this.cardElement = this.elements.create('card', {
      style: {
        base: {
          fontSize: '15px',
          color: '#111827',
          '::placeholder': { color: '#9CA3AF' }
        },
        invalid: { color: '#DC2626' }
      }
    });
    this.cardElement.mount(`#${containerId}`);
    return this.cardElement;
  }

  unmountCardElement(): void {
    this.cardElement?.unmount();
    this.cardElement = null;
    this.elements = null;
  }

  async confirmCardPayment(clientSecret: string, cardholderName: string): Promise<{ paymentIntentId?: string; error?: string }> {
    const stripe = await this.getStripe();
    if (!stripe || !this.cardElement) {
      return { error: 'Payment form is not ready yet. Please wait a moment and try again.' };
    }

    const result = await stripe.confirmCardPayment(clientSecret, {
      payment_method: {
        card: this.cardElement,
        billing_details: { name: cardholderName }
      }
    });

    if (result.error) {
      return { error: result.error.message || 'Your card was declined.' };
    }

    if (result.paymentIntent?.status !== 'succeeded') {
      return { error: `Payment was not completed (status: ${result.paymentIntent?.status}).` };
    }

    return { paymentIntentId: result.paymentIntent.id };
  }
}
