import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';
import { CreateCourseRequest } from '@shared/models/course.model';

@Component({
  selector: 'app-create-course',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './create-course.component.html',
  styleUrl: './create-course.component.scss'
})
export class CreateCourseComponent implements OnInit, OnDestroy {
  courseForm!: FormGroup;
  submitting = false;
  error: string | null = null;
  success: string | null = null;

  categories = ['Web Development', 'Mobile Development', 'Data Science', 'AI & Machine Learning', 'Cloud Computing', 'DevOps'];
  levels = ['Beginner', 'Intermediate', 'Advanced'];

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private courseService: CourseService,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.courseForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(20)]],
      category: ['', Validators.required],
      level: ['Beginner', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      thumbnailUrl: ['']
    });
  }

  onSubmit(): void {
    if (this.courseForm.invalid) return;

    this.submitting = true;
    this.error = null;
    this.success = null;

    const request: CreateCourseRequest = this.courseForm.value;

    this.courseService.createCourse(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (course) => {
          this.submitting = false;
          this.toast.success('Course created! Now add your content.');
          this.router.navigate(['/instructor/course-builder', course.id]);
        },
        error: (err) => {
          this.submitting = false;
          this.error = err.error?.message || 'Failed to create course.';
          this.toast.error(this.error!);
        }
      });
  }

  get title() { return this.courseForm.get('title'); }
  get description() { return this.courseForm.get('description'); }
  get price() { return this.courseForm.get('price'); }
  get category() { return this.courseForm.get('category'); }
  get level() { return this.courseForm.get('level'); }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
