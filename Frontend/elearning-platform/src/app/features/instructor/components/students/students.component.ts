import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { InstructorService } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';
import { EnrolledStudent, Course } from '@shared/models/course.model';

@Component({
  selector: 'app-students',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './students.component.html',
  styleUrl: './students.component.scss'
})
export class StudentsComponent implements OnInit, OnDestroy {
  students: EnrolledStudent[] = [];
  course: Course | null = null;
  courseId = 0;
  loading = false;
  searchTerm = '';

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private instructorService: InstructorService,
    private courseService: CourseService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadData();
    });
  }

  loadData(): void {
    this.loading = true;

    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: course => (this.course = course) });

    this.instructorService.getEnrolledStudents(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: students => {
          this.students = students;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load students.');
        }
      });
  }

  get filteredStudents(): EnrolledStudent[] {
    if (!this.searchTerm.trim()) return this.students;
    const q = this.searchTerm.toLowerCase();
    return this.students.filter(s =>
      s.studentName?.toLowerCase().includes(q) ||
      s.studentEmail?.toLowerCase().includes(q)
    );
  }

  get avgCompletion(): number {
    if (!this.students.length) return 0;
    return Math.round(this.students.reduce((sum, s) => sum + s.completionPercentage, 0) / this.students.length);
  }

  get completedCount(): number {
    return this.students.filter(s => s.completionPercentage >= 100).length;
  }

  get totalRevenue(): number {
    return this.students.reduce((sum, s) => sum + s.pricePaid, 0);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
