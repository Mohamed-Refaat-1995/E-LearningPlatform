import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gray-50">
      <div class="max-w-7xl mx-auto py-8 px-4">
        <a routerLink="/admin" class="text-indigo-600 hover:text-indigo-700 font-medium mb-4 inline-block">← Back</a>
        <h1 class="text-3xl font-bold text-gray-900 mt-4">Reports</h1>
        <p class="text-gray-600 mt-2">View detailed analytics and reports</p>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mt-8">
          <div class="bg-white rounded-lg shadow p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-4">User Statistics</h3>
            <p class="text-gray-600">Analytics coming soon...</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-4">Revenue Statistics</h3>
            <p class="text-gray-600">Analytics coming soon...</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-4">Course Performance</h3>
            <p class="text-gray-600">Analytics coming soon...</p>
          </div>
          <div class="bg-white rounded-lg shadow p-6">
            <h3 class="text-lg font-bold text-gray-900 mb-4">Student Progress</h3>
            <p class="text-gray-600">Analytics coming soon...</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ReportsComponent {}
