import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';

export interface CouponValidationResult {
  discountAmount: number;
  finalPrice: number;
}

@Injectable({ providedIn: 'root' })
export class CouponService {
  private get API_URL() { return `${this.config.apiUrl}/coupons`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  validateCoupon(code: string, courseId: number, originalPrice: number): Observable<CouponValidationResult> {
    return this.http.post<CouponValidationResult>(`${this.API_URL}/validate`, { code, courseId, originalPrice });
  }

  getInstructorCoupons(): Observable<any[]> {
    return this.http.get<any[]>(this.API_URL);
  }

  createCoupon(data: any): Observable<any> {
    return this.http.post(this.API_URL, data);
  }

  deactivateCoupon(id: number): Observable<any> {
    return this.http.patch(`${this.API_URL}/${id}/deactivate`, {});
  }

  deleteCoupon(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${id}`);
  }
}
