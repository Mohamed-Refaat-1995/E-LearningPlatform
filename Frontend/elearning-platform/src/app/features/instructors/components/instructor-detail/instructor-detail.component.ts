import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { InstructorService } from '@core/services/instructor.service';
import { PublicInstructor } from '@shared/models/course.model';

interface InstructorCourseSummary {
  id: number;
  title: string;
  description: string;
  thumbnailUrl?: string;
  price: number;
  isPublished: boolean;
}

@Component({
  selector: 'app-instructor-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './instructor-detail.component.html',
  styleUrl: './instructor-detail.component.scss'
})
export class InstructorDetailComponent implements OnInit {
  instructor: PublicInstructor | null = null;
  courses: InstructorCourseSummary[] = [];
  loading = true;
  notFound = false;

  constructor(
    private route: ActivatedRoute,
    private instructorService: InstructorService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.instructorService.getPublicInstructorById(id).subscribe({
      next: instructor => {
        this.instructor = instructor;
        this.loading = false;
      },
      error: () => {
        this.notFound = true;
        this.loading = false;
      }
    });

    this.instructorService.getInstructorCourses(id).subscribe({
      next: courses => {
        this.courses = (courses || []).filter((c: InstructorCourseSummary) => c.isPublished);
      },
      error: () => { this.courses = []; }
    });
  }

  initials(name: string): string {
    return name.split(' ').filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase();
  }

  stars(rating: number): string {
    const r = Math.round(rating || 0);
    return '★'.repeat(r) + '☆'.repeat(Math.max(0, 5 - r));
  }
}
