import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payment, Invoice, PaymentRequest } from '@shared/models/payment.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private readonly API_URL = `${environment.apiUrl}/payments`;
  private readonly INVOICES_URL = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  createPayment(request: PaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(this.API_URL, request);
  }

  processPayment(paymentId: number, stripePaymentIntentId: string): Observable<any> {
    return this.http.post(`${this.API_URL}/${paymentId}/process`, { stripePaymentIntentId });
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
