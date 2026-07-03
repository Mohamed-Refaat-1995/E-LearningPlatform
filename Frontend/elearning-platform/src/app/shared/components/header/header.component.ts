import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { InstructorService, InstructorSearchSuggestions } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { CartService } from '@core/services/cart.service';
import { NotificationService } from '@core/services/notification.service';
import { User, UserRole } from '@shared/models/user.model';
import { Category, Course } from '@shared/models/course.model';
import { Router } from '@angular/router';
import { ThemeToggleComponent } from '@shared/components/theme-toggle/theme-toggle.component';
import { CoursePreviewModalComponent } from '@shared/components/course-preview-modal/course-preview-modal.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ThemeToggleComponent, CoursePreviewModalComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  UserRole = UserRole;

  showUserMenu = false;
  showCategoriesMenu = false;
  showMobileMenu = false;

  // Instructor navbar search
  searchQuery = '';
  showSearchDropdown = false;
  searchSuggestions: InstructorSearchSuggestions | null = null;

  // Student navbar search
  courseSuggestions: Course[] = [];
  selectedPreviewCourse: Course | null = null;
  showCoursePreview = false;

  private searchInput$ = new Subject<string>();

  categories: Category[] = [];

  unreadNotificationCount = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private instructorService: InstructorService,
    private courseService: CourseService,
    public cartService: CartService,
    private notificationService: NotificationService,
    private router: Router,
    private elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
        if (user?.role === UserRole.Student) {
          this.notificationService.connect();
        } else {
          this.notificationService.disconnect();
        }
      });

    this.notificationService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(list => this.unreadNotificationCount = list.filter(n => !n.isRead).length);

    this.courseService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: cats => this.categories = cats,
        error: () => this.categories = []
      });

    this.searchInput$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(q => {
      if (!q.trim()) {
        this.searchSuggestions = null;
        this.courseSuggestions = [];
        this.showSearchDropdown = false;
        return;
      }

      if (this.isInstructor) {
        this.instructorService.searchSuggestions(q.trim()).subscribe({
          next: res => {
            this.searchSuggestions = res;
            this.showSearchDropdown = true;
          },
          error: () => { this.searchSuggestions = null; }
        });
      } else if (this.isStudent) {
        this.courseService.searchCourses(q.trim()).subscribe({
          next: res => {
            this.courseSuggestions = res;
            this.showSearchDropdown = true;
          },
          error: () => { this.courseSuggestions = []; }
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get isGuest(): boolean { return !this.currentUser; }
  get isStudent(): boolean { return this.currentUser?.role === UserRole.Student; }
  get isInstructor(): boolean { return this.currentUser?.role === UserRole.Instructor; }
  get isAdmin(): boolean { return this.currentUser?.role === UserRole.Admin; }

  get userDisplayName(): string {
    if (!this.currentUser) return '';
    const first = this.currentUser.firstName?.trim();
    const last = this.currentUser.lastName?.trim();
    if (first || last) return `${first} ${last}`.trim();
    return this.currentUser.email.split('@')[0];
  }

  logout(): void {
    this.authService.logout();
    this.notificationService.disconnect();
    this.router.navigate(['/auth/login']);
    this.showUserMenu = false;
    this.showMobileMenu = false;
  }

  toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.showUserMenu = !this.showUserMenu;
    this.showCategoriesMenu = false;
  }

  toggleCategoriesMenu(event: Event): void {
    event.stopPropagation();
    this.showCategoriesMenu = !this.showCategoriesMenu;
  }

  closeUserMenu(): void {
    this.showUserMenu = false;
  }

  closeCategoriesMenu(): void {
    this.showCategoriesMenu = false;
  }

  toggleMobileMenu(): void {
    this.showMobileMenu = !this.showMobileMenu;
  }

  closeMobileMenu(): void {
    this.showMobileMenu = false;
    this.router.navigate(['/']);
  }

  @HostListener('document:click', ['$event'])
  closeDropdowns(event: Event): void {
    // Only close when the click happened outside this header component.
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.showUserMenu = false;
      this.showCategoriesMenu = false;
      this.showSearchDropdown = false;
    }
  }

  onSearchInput(value: string): void {
    this.searchQuery = value;
    this.searchInput$.next(value);
  }

  hasSearchResults(): boolean {
    const s = this.searchSuggestions;
    return !!s && (s.courses.length > 0 || s.students.length > 0 || s.reviews.length > 0);
  }

  goToCourse(courseId: number): void {
    this.showSearchDropdown = false;
    this.router.navigate(['/instructor/course-builder', courseId]);
  }

  goToStudent(name: string): void {
    this.showSearchDropdown = false;
    this.router.navigate(['/instructor/students'], { queryParams: { search: name } });
  }

  goToReview(courseId: number, snippet: string): void {
    this.showSearchDropdown = false;
    this.router.navigate(['/instructor/reviews'], { queryParams: { search: snippet } });
  }

  openCoursePreview(course: Course): void {
    this.showSearchDropdown = false;
    this.selectedPreviewCourse = course;
    this.showCoursePreview = true;
  }

  closeCoursePreview(): void {
    this.showCoursePreview = false;
  }
}
