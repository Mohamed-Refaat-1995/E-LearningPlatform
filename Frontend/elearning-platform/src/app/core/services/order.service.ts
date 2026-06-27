import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order, CreateOrderRequest } from '@shared/models/payment.model';
import { ConfigService } from './config.service';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private get API_URL() { return `${this.config.apiUrl}/orders`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

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
