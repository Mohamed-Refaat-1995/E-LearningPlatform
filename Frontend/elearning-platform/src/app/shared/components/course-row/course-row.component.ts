import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CourseCardComponent } from '../course-card/course-card.component';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-course-row',
  standalone: true,
  imports: [CommonModule, CourseCardComponent],
  templateUrl: './course-row.component.html',
  styleUrl: './course-row.component.scss'
})
export class CourseRowComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() courses: Course[] = [];
  @Input() loading = false;
  @Input() compact = false;
  @Input() redirectGuestToAuth = false;

  trackId(_: number, c: Course) { return c.id; }
}
