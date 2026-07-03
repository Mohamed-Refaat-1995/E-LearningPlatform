import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { InstructorService, InstructorReviewGridItem } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reviews.component.html',
  styleUrl: './reviews.component.scss'
})
export class ReviewsComponent implements OnInit, OnDestroy {
  readonly reactionEmojis = ['👍', '❤️', '😘', '😂', '🎉'];

  rows: InstructorReviewGridItem[] = [];
  loading = false;

  search = '';
  ratingFilter: number | '' = '';
  sortBy = 'date';
  sortDir: 'asc' | 'desc' = 'desc';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  replyDraft: { [reviewId: number]: string } = {};
  savingReplyId: number | null = null;

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(
    private instructorService: InstructorService,
    private courseService: CourseService,
    private toast: ToastService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.search$.pipe(debounceTime(350), takeUntil(this.destroy$)).subscribe(() => {
      this.page = 1;
      this.load();
    });
    this.search = this.route.snapshot.queryParamMap.get('search') || '';
    this.load();
  }

  load(): void {
    this.loading = true;
    this.instructorService.getReviewsGrid({
      search: this.search || undefined,
      rating: this.ratingFilter === '' ? undefined : this.ratingFilter,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.rows = res.items;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.loading = false;
          for (const r of res.items) {
            if (r.instructorReply && this.replyDraft[r.id] === undefined) {
              this.replyDraft[r.id] = r.instructorReply;
            }
          }
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load reviews.');
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  sortByColumn(col: string): void {
    if (this.sortBy === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = col;
      this.sortDir = 'asc';
    }
    this.load();
  }

  sortIcon(col: string): string {
    if (this.sortBy !== col) return '';
    return this.sortDir === 'asc' ? '▲' : '▼';
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.load();
  }

  reactionCount(review: InstructorReviewGridItem, emoji: string): number {
    return review.reactionCounts?.[emoji] || 0;
  }

  toggleReaction(review: InstructorReviewGridItem, emoji: string): void {
    this.courseService.reactToReview(review.id, emoji)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          review.reactionCounts = res.reactionCounts;
          review.myReaction = res.myReaction || undefined;
        }
      });
  }

  saveReply(review: InstructorReviewGridItem): void {
    const reply = (this.replyDraft[review.id] || '').trim();
    this.savingReplyId = review.id;
    this.instructorService.replyToReview(review.id, reply)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          review.instructorReply = res.instructorReply;
          review.repliedAt = res.repliedAt;
          this.savingReplyId = null;
          this.toast.success(reply ? 'Reply saved.' : 'Reply removed.');
        },
        error: () => {
          this.savingReplyId = null;
          this.toast.error('Failed to save reply.');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
