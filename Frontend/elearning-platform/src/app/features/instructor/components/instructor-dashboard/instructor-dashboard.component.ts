import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gray-50">
      <div class="max-w-7xl mx-auto py-8 px-4">
        <h1 class="text-3xl font-bold text-gray-900 mb-8">Instructor Dashboard</h1>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
          <div class="bg-white rounded-lg shadow p-6">
            <p class="text-gray-600 text-sm">Total Courses</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">0</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <p class="text-gray-600 text-sm">Total Students</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">0</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <p class="text-gray-600 text-sm">Total Revenue</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">$0.00</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <p class="text-gray-600 text-sm">Avg. Rating</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">0.0</p>
          </div>
        </div>

        <div class="bg-white rounded-lg shadow p-6">
          <div class="flex justify-between items-center mb-6">
            <h2 class="text-2xl font-bold text-gray-900">My Courses</h2>
            <a routerLink="/instructor/create-course" class="bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-2 rounded-lg font-medium transition">
              Create Course
            </a>
          </div>
          <p class="text-gray-600">No courses created yet</p>
        </div>
      </div>
    </div>
  `
})
export class InstructorDashboardComponent implements OnInit {
  ngOnInit(): void {}
}
