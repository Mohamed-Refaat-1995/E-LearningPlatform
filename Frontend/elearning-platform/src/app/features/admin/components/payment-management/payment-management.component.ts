import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-payment-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gray-50">
      <div class="max-w-7xl mx-auto py-8 px-4">
        <a routerLink="/admin" class="text-indigo-600 hover:text-indigo-700 font-medium mb-4 inline-block">← Back</a>
        <h1 class="text-3xl font-bold text-gray-900 mt-4">Payment Management</h1>
        <p class="text-gray-600 mt-2">View transactions and manage refunds</p>

        <div class="bg-white rounded-lg shadow p-8 mt-8">
          <p class="text-gray-600">Payment management interface coming soon...</p>
        </div>
      </div>
    </div>
  `
})
export class PaymentManagementComponent {}
