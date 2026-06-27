import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  totalUsers = 0;
  totalCourses = 0;
  totalRevenue = 0;
  pendingApprovals = 0;
  loading = false;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;

    forkJoin({
      students: this.adminService.getStudents().pipe(catchError(() => of([]))),
      instructors: this.adminService.getInstructors().pipe(catchError(() => of([]))),
      courses: this.adminService.getAllCourses().pipe(catchError(() => of([]))),
      orders: this.adminService.getAllOrders().pipe(catchError(() => of([])))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ students, instructors, courses, orders }) => {
          this.totalUsers = (students?.length || 0) + (instructors?.length || 0);
          this.totalCourses = courses?.length || 0;
          this.pendingApprovals = (courses || []).filter((c: any) => !c.isPublished).length;
          this.totalRevenue = (orders || []).reduce((sum: number, o: any) => sum + (o.totalAmount || 0), 0);
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
