import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit, OnDestroy {
  totalStudents = 0;
  totalInstructors = 0;
  totalCourses = 0;
  publishedCourses = 0;
  totalRevenue = 0;
  totalOrders = 0;
  paidOrders = 0;
  loading = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      students: this.adminService.getStudents().pipe(catchError(() => of([]))),
      instructors: this.adminService.getInstructors().pipe(catchError(() => of([]))),
      courses: this.adminService.getAllCourses().pipe(catchError(() => of([]))),
      orders: this.adminService.getAllOrders().pipe(catchError(() => of([])))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ students, instructors, courses, orders }) => {
          this.totalStudents = students?.length || 0;
          this.totalInstructors = instructors?.length || 0;
          this.totalCourses = courses?.length || 0;
          this.publishedCourses = (courses || []).filter((c: any) => c.isPublished).length;
          this.totalOrders = orders?.length || 0;
          this.paidOrders = (orders || []).filter((o: any) => o.status === 'Paid').length;
          this.totalRevenue = (orders || [])
            .filter((o: any) => o.status === 'Paid')
            .reduce((sum: number, o: any) => sum + (o.totalAmount || 0), 0);
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load reports.';
          this.loading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
