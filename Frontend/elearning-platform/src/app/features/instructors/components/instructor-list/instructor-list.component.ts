import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { InstructorService } from '@core/services/instructor.service';
import { PublicInstructor } from '@shared/models/course.model';

@Component({
  selector: 'app-instructor-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './instructor-list.component.html',
  styleUrl: './instructor-list.component.scss'
})
export class InstructorListComponent implements OnInit {
  instructors: PublicInstructor[] = [];
  loading = true;
  error: string | null = null;

  constructor(private instructorService: InstructorService) {}

  ngOnInit(): void {
    this.instructorService.getPublicInstructors().subscribe({
      next: list => { this.instructors = list; this.loading = false; },
      error: () => { this.error = 'Failed to load instructors.'; this.loading = false; }
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
