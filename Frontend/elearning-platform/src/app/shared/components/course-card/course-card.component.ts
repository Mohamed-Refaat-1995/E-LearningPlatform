import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Course } from '@shared/models/course.model';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-course-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './course-card.component.html',
  styleUrl: './course-card.component.scss'
})
export class CourseCardComponent {
  @Input({ required: true }) course!: Course;
  @Input() redirectGuestToAuth = false;

  constructor(private router: Router, private authService: AuthService) {}

  onCardClick(event: MouseEvent): void {
    if (this.redirectGuestToAuth && !this.authService.getCurrentUserSnapshot()) {
      event.preventDefault();
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.course.id}` } });
    }
  }

  get stars(): string {
    const r = Math.round(this.course.averageRating || 0);
    return '★'.repeat(r) + '☆'.repeat(Math.max(0, 5 - r));
  }

  get isBestseller(): boolean {
    return (this.course.totalStudents ?? 0) > 100;
  }

  get isPremium(): boolean {
    return (this.course.price ?? 0) >= 100;
  }
}
