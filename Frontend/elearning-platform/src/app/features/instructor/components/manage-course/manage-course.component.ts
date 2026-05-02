import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-manage-course',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gray-50">
      <div class="max-w-6xl mx-auto py-8 px-4">
        <a routerLink="/instructor" class="text-indigo-600 hover:text-indigo-700 font-medium mb-4 inline-block">← Back</a>
        <h1 class="text-3xl font-bold text-gray-900 mt-4">Manage Course</h1>

        <div class="bg-white rounded-lg shadow p-8 mt-8">
          <p class="text-gray-600">Course management interface coming soon...</p>
        </div>
      </div>
    </div>
  `
})
export class ManageCourseComponent implements OnInit {
  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const courseId = params['courseId'];
      // Load course for editing
    });
  }
}
