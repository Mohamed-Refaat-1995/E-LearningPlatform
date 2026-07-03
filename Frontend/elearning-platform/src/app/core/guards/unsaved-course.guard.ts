import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

export interface UnsavedCourseComponent {
  confirmExit(): Observable<boolean> | boolean;
}

export const unsavedCourseGuard: CanDeactivateFn<UnsavedCourseComponent> = (component) => {
  return component.confirmExit();
};
