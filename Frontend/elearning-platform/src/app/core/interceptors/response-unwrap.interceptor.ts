import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

/**
 * Backend wraps almost every response in a GenericResponseDTO envelope:
 *   { isSuccess: boolean, data: T | null, message: string }
 *
 * Services (CourseService, InstructorService, AdminService, ...) type their
 * calls against the raw payload (e.g. `Course[]`), so this interceptor unwraps
 * the envelope centrally and hands `data` back to them.
 *
 * Auth endpoints are intentionally skipped: AuthService consumes the full
 * envelope itself (it reads `isSuccess` / `message` for UX and unwraps `data`
 * where needed), so unwrapping here would break those flows.
 */
@Injectable()
export class ResponseUnwrapInterceptor implements HttpInterceptor {

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (this.isAuthRequest(request.url)) {
      return next.handle(request);
    }

    return next.handle(request).pipe(
      map(event => {
        if (event instanceof HttpResponse && this.isEnvelope(event.body)) {
          return event.clone({ body: event.body.data });
        }
        return event;
      })
    );
  }

  private isAuthRequest(url: string): boolean {
    return url.includes('/auth/');
  }

  /** True when the body matches the GenericResponseDTO shape. */
  private isEnvelope(body: any): boolean {
    return !!body
      && typeof body === 'object'
      && typeof body.isSuccess === 'boolean'
      && 'data' in body
      && 'message' in body;
  }
}
