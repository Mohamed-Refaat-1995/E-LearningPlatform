import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { User, UserRole } from '@shared/models/user.model';
import { Router } from '@angular/router';
import { ThemeToggleComponent } from '@shared/components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, ThemeToggleComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  UserRole = UserRole;

  showUserMenu = false;
  showCategoriesMenu = false;
  showMobileMenu = false;

  categories: string[] = [
    'Web Development',
    'Mobile Development',
    'Data Science',
    'AI & Machine Learning',
    'Cloud Computing',
    'DevOps'
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private router: Router,
    private elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => this.currentUser = user);
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
    }
  }
}
