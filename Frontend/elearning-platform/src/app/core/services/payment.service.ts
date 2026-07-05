import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payment, Invoice, PaymentRequest, ProcessPaymentRequest, PaymentWithClientSecret } from '@shared/models/payment.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private get API_URL() { return `${this.config.apiUrl}/payments`; }
  private get INVOICES_URL() { return `${this.config.apiUrl}/invoices`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  createPayment(request: PaymentRequest): Observable<PaymentWithClientSecret> {
    return this.http.post<PaymentWithClientSecret>(this.API_URL, request);
  }

  processPayment(paymentId: number, stripePaymentIntentId: string): Observable<any> {
    const body: ProcessPaymentRequest = { stripePaymentIntentId };
    return this.http.post(`${this.API_URL}/${paymentId}/process`, body);
  }

  getPayment(id: number): Observable<Payment> {
    return this.http.get<Payment>(`${this.API_URL}/${id}`);
  }

  getUserPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(this.API_URL);
  }

  generateInvoice(paymentId: number): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.INVOICES_URL}`, { paymentId });
  }

  getInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(this.INVOICES_URL);
  }

  getInvoice(id: number): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.INVOICES_URL}/${id}`);
  }

  refundPayment(paymentId: number): Observable<any> {
    return this.http.post(`${this.API_URL}/${paymentId}/refund`, {});
  }

  handleWebhook(event: any): Observable<any> {
    return this.http.post(`${this.API_URL}/webhook`, event);
  }
}
