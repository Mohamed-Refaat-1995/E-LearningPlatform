import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-course-card',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './course-card.component.html',
  styleUrl: './course-card.component.scss'
})
export class CourseCardComponent {
  @Input({ required: true }) course!: Course;

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
