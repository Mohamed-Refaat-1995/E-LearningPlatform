import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';
import { Review, Course } from '@shared/models/course.model';

interface ReviewWithReply extends Review {
  replyText?: string;
  showReplyBox?: boolean;
  replied?: boolean;
}

@Component({
  selector: 'app-qa',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './qa.component.html'
})
export class QaComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  reviews: ReviewWithReply[] = [];
  loading = false;
  courseId = 0;
  filterRating: number | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private courseService: CourseService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = +params['courseId'];
      this.loadData();
    });
  }

  loadData(): void {
    this.loading = true;
    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: c => {
          this.course = c;
          this.loadReviews();
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load course.');
        }
      });
  }

  loadReviews(): void {
    this.courseService.getCourseReviews(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (reviews: Review[]) => {
          this.reviews = reviews.map(r => ({ ...r, replyText: '', showReplyBox: false, replied: false }));
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load student reviews.');
        }
      });
  }

  get filteredReviews(): ReviewWithReply[] {
    if (this.filterRating === null) return this.reviews;
    return this.reviews.filter(r => r.rating === this.filterRating);
  }

  get averageRating(): number {
    if (!this.reviews.length) return 0;
    return this.reviews.reduce((s, r) => s + r.rating, 0) / this.reviews.length;
  }

  ratingCount(star: number): number {
    return this.reviews.filter(r => r.rating === star).length;
  }

  ratingPercent(star: number): number {
    if (!this.reviews.length) return 0;
    return Math.round((this.ratingCount(star) / this.reviews.length) * 100);
  }

  toggleReplyBox(review: ReviewWithReply): void {
    review.showReplyBox = !review.showReplyBox;
  }

  submitReply(review: ReviewWithReply): void {
    if (!review.replyText?.trim()) {
      this.toast.error('Reply cannot be empty.');
      return;
    }
    // Visual-only reply (no backend endpoint for instructor replies yet)
    review.replied = true;
    review.showReplyBox = false;
    this.toast.success('Reply saved! (Visible to you only — backend reply endpoint coming soon)');
  }

  stars(n: number): number[] {
    return Array.from({ length: n }, (_, i) => i + 1);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
