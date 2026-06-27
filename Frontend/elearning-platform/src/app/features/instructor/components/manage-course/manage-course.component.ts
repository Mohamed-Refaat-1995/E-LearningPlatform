import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { InstructorService } from '@core/services/instructor.service';
import { ToastService } from '@core/services/toast.service';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-manage-course',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './manage-course.component.html',
  styleUrl: './manage-course.component.scss'
})
export class ManageCourseComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  courseForm!: FormGroup;
  loading = false;
  submitting = false;
  deleting = false;
  publishing = false;
  showDeleteConfirm = false;

  categories = ['Web Development', 'Mobile Development', 'Data Science', 'AI & Machine Learning', 'Cloud Computing', 'DevOps', 'Cybersecurity', 'Business', 'Design', 'Marketing'];
  levels = ['Beginner', 'Intermediate', 'Advanced'];

  courseId = 0;
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private courseService: CourseService,
    private instructorService: InstructorService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.courseForm = this.fb.group({
      title:        ['', [Validators.required, Validators.minLength(5)]],
      description:  ['', [Validators.required, Validators.minLength(20)]],
      category:     ['', Validators.required],
      level:        ['Beginner', Validators.required],
      price:        [0, [Validators.required, Validators.min(0)]],
      thumbnailUrl: ['']
    });

    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadCourse();
    });
  }

  loadCourse(): void {
    this.loading = true;
    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: course => {
          this.course = course;
          this.courseForm.patchValue({
            title:        course.title,
            description:  course.description,
            category:     course.category,
            level:        course.level,
            price:        course.price,
            thumbnailUrl: course.thumbnailUrl || ''
          });
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load course.');
        }
      });
  }

  onSubmit(): void {
    if (this.courseForm.invalid) return;
    this.submitting = true;

    this.courseService.updateCourse(this.courseId, this.courseForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: updated => {
          this.submitting = false;
          if (this.course) Object.assign(this.course, updated);
          this.toast.success('Course updated successfully!');
        },
        error: err => {
          this.submitting = false;
          this.toast.error(err.error?.message || 'Failed to update course.');
        }
      });
  }

  togglePublish(): void {
    if (!this.course) return;
    this.publishing = true;
    this.instructorService.togglePublish(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.publishing = false;
          if (this.course) this.course.isPublished = res.isPublished;
          this.toast.success(res.isPublished ? 'Course published!' : 'Course unpublished.');
        },
        error: () => {
          this.publishing = false;
          this.toast.error('Failed to update publish status.');
        }
      });
  }

  confirmDelete(): void {
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
  }

  executeDelete(): void {
    this.deleting = true;
    this.courseService.deleteCourse(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Course deleted successfully.');
          this.router.navigate(['/instructor']);
        },
        error: err => {
          this.deleting = false;
          this.showDeleteConfirm = false;
          const msg = err.error?.message || 'Failed to delete course.';
          if (msg.includes('enrolled')) {
            this.toast.error('Cannot delete: this course has enrolled students.');
          } else {
            this.toast.error(msg);
          }
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
