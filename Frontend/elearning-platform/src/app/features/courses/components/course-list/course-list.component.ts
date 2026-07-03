import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { EnrollmentService } from '@core/services/enrollment.service';
import { CartService } from '@core/services/cart.service';
import { Course, Category } from '@shared/models/course.model';
import { AuthService } from '@core/services/auth.service';
import { UserRole } from '@shared/models/user.model';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './course-list.component.html',
  styleUrl: './course-list.component.scss'
})
export class CourseListComponent implements OnInit, OnDestroy {
  courses: Course[] = [];
  filteredCourses: Course[] = [];
  loading = false;
  error: string | null = null;

  filterForm!: FormGroup;
  currentPage = 1;
  pageSize = 12;
  totalCourses = 0;

  categories: Category[] = [];
  levels: string[] = ['Beginner', 'Intermediate', 'Advanced'];
  activeCategory: string | null = null;

  enrolledCourseIds = new Set<number>();

  private destroy$ = new Subject<void>();

  constructor(
    private courseService: CourseService,
    private formBuilder: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private enrollmentService: EnrollmentService,
    private cartService: CartService
  ) {}

  ngOnInit(): void {
    this.filterForm = this.formBuilder.group({
      searchTerm: [''],
      category: [''],
      level: [''],
      minPrice: [''],
      maxPrice: ['']
    });

    this.courseService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: cats => this.categories = cats, error: () => this.categories = [] });

    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const category = params['category'] || '';
      this.activeCategory = category || null;
      this.filterForm.patchValue({ category }, { emitEvent: false });
      if (category) this.applyFilters();
    });

    this.loadCourses();
    this.setupFilterListeners();
    this.loadEnrollments();
  }

  loadEnrollments(): void {
    if (this.authService.getCurrentUserSnapshot()?.role !== UserRole.Student) return;
    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: list => this.enrolledCourseIds = new Set(list.map((e: any) => Number(e.courseId))),
        error: () => this.enrolledCourseIds = new Set()
      });
  }

  isEnrolled(courseId: number): boolean {
    return this.enrolledCourseIds.has(courseId);
  }

  isStudent(): boolean {
    return this.authService.getCurrentUserSnapshot()?.role === UserRole.Student;
  }

  goToCourse(courseId: number, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/dashboard/my-courses', courseId]);
  }

  addToCart(course: Course, event: Event): void {
    event.stopPropagation();
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: '/courses' } });
      return;
    }
    this.cartService.addToCart({
      courseId: course.id,
      title: course.title,
      thumbnailUrl: course.thumbnailUrl,
      price: course.price,
      instructorName: course.instructorName
    });
  }

  isInCart(courseId: number): boolean {
    return this.cartService.isInCart(courseId);
  }

  loadCourses(): void {
    this.loading = true;
    this.error = null;

    this.courseService.getAllCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses) => {
          this.courses = courses;
          this.filteredCourses = courses;
          this.totalCourses = courses.length;
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to load courses. Please try again.';
          this.loading = false;
        }
      });
  }

  setupFilterListeners(): void {
    this.filterForm.get('searchTerm')?.valueChanges
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(searchTerm => {
        if (searchTerm && searchTerm.trim()) {
          this.searchCourses(searchTerm);
        } else {
          this.applyFilters();
        }
      });

    this.filterForm.valueChanges
      .pipe(
        debounceTime(500),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.applyFilters();
      });
  }

  searchCourses(searchTerm: string): void {
    this.loading = true;
    this.courseService.searchCourses(searchTerm)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses) => {
          this.filteredCourses = courses;
          this.totalCourses = courses.length;
          this.currentPage = 1;
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Search failed. Please try again.';
          this.loading = false;
        }
      });
  }

  applyFilters(): void {
    const { category, level, minPrice, maxPrice } = this.filterForm.value;

    if (!category && !level && !minPrice && !maxPrice) {
      this.filteredCourses = this.courses;
      return;
    }

    this.loading = true;
    this.courseService.filterCourses(
      category || '',
      level || '',
      minPrice ? parseFloat(minPrice) : undefined,
      maxPrice ? parseFloat(maxPrice) : undefined,
      this.currentPage,
      this.pageSize
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.filteredCourses = Array.isArray(response) ? response : (response.items || []);
          this.totalCourses = Array.isArray(response) ? response.length : (response.totalCount || 0);
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Filter failed. Please try again.';
          this.loading = false;
        }
      });
  }

  viewCourse(courseId: number): void {
    this.router.navigate(['/courses', courseId]);
  }

  getRatingStars(rating: number): number[] {
    return Array(Math.round(rating)).fill(0);
  }

  get paginatedCourses(): Course[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredCourses.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.totalCourses / this.pageSize);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
