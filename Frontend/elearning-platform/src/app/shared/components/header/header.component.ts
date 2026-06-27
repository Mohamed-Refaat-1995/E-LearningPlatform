import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { User } from '@shared/models/user.model';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit {
  user: User | null = null;
  isAuthed = false;
  searchTerm = '';

  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.auth.getCurrentUser$().subscribe(u => (this.user = u));
    this.auth.isAuthenticated().subscribe(a => (this.isAuthed = a));
  }

  onSearch(e: Event) {
    e.preventDefault();
    const term = this.searchTerm.trim();
    if (!term) return;
    this.router.navigate(['/courses'], { queryParams: { q: term } });
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
