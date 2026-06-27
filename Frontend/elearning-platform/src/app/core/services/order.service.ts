import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order, CreateOrderRequest } from '@shared/models/payment.model';
import { environment } from '@environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly API_URL = `${environment.apiUrl}/orders`;

  constructor(private http: HttpClient) {}

  createOrder(courseIds: number[]): Observable<Order> {
    return this.http.post<Order>(this.API_URL, { courseIds } as CreateOrderRequest);
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.API_URL}/me`);
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.API_URL}/${id}`);
  }

  cancelOrder(id: number): Observable<any> {
    return this.http.post(`${this.API_URL}/${id}/cancel`, {});
  }
}
