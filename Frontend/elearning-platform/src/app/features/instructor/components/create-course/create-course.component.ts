import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { InstructorService } from '@core/services/instructor.service';
import { ToastService } from '@core/services/toast.service';
import { CreateCourseRequest, Category } from '@shared/models/course.model';

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

  categories: Category[] = [];
  levels = ['Beginner', 'Intermediate', 'Advanced'];

  profitPercentage = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private courseService: CourseService,
    private instructorService: InstructorService,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.courseForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', [Validators.required, Validators.minLength(20)]],
      categoryId: [null, Validators.required],
      level: ['Beginner', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      thumbnailUrl: [''],
      feeConfirmed: [false, Validators.requiredTrue]
    });

    this.courseService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: categories => this.categories = categories,
        error: () => this.toast.error('Failed to load categories.')
      });

    this.instructorService.getPlatformProfitPercentage()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => this.profitPercentage = res.profitPercentage,
        error: () => { /* fee preview simply won't show if unavailable */ }
      });
  }

  get platformFee(): number {
    const price = Number(this.price?.value) || 0;
    return Math.round(price * this.profitPercentage) / 100;
  }

  get instructorShare(): number {
    const price = Number(this.price?.value) || 0;
    return Math.round((price - this.platformFee) * 100) / 100;
  }

  onSubmit(): void {
    if (this.courseForm.invalid) return;

    this.submitting = true;
    this.error = null;
    this.success = null;

    const { feeConfirmed, ...formValue } = this.courseForm.value;
    const request: CreateCourseRequest = formValue;

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
  get categoryId() { return this.courseForm.get('categoryId'); }
  get level() { return this.courseForm.get('level'); }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
