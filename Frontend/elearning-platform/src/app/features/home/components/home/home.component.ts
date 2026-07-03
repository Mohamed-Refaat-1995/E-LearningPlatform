import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { CourseService } from '@core/services/course.service';
import { Course } from '@shared/models/course.model';
import { User } from '@shared/models/user.model';
import { CourseRowComponent } from '@shared/components/course-row/course-row.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, CourseRowComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  user: User | null = null;
  recommended: Course[] = [];
  popular: Course[] = [];
  topRated: Course[] = [];
  loadingRecommended = true;
  loadingPopular = true;
  loadingTopRated = true;

  constructor(
    private auth: AuthService,
    private courseService: CourseService
  ) {}

  ngOnInit(): void {
    this.auth.getCurrentUser$().subscribe(u => (this.user = u));

    this.courseService.getRecommended(10).subscribe({
      next: c => { this.recommended = c; this.loadingRecommended = false; },
      error: () => { this.loadingRecommended = false; }
    });

    this.courseService.getPopular(10).subscribe({
      next: c => { this.popular = c; this.loadingPopular = false; },
      error: () => { this.loadingPopular = false; }
    });

    this.courseService.getTopRated(10).subscribe({
      next: c => { this.topRated = c; this.loadingTopRated = false; },
      error: () => { this.loadingTopRated = false; }
    });
  }

  get firstName(): string {
    return this.user?.firstName || this.user?.email?.split('@')[0] || '';
  }

  get initials(): string {
    if (!this.user) return '?';
    const first = (this.user.firstName?.[0] || this.user.email?.[0] || '?').toUpperCase();
    const last = (this.user.lastName?.[0] || '').toUpperCase();
    return first + last;
  }
}
