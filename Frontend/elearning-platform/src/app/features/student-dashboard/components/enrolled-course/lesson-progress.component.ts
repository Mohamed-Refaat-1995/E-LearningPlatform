import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lesson-progress',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './lesson-progress.component.html',
  styleUrl: './lesson-progress.component.scss'
})
export class LessonProgressComponent {
  @Input() progress: any;

  get progressPercentage(): number {
    if (!this.progress || !this.progress.durationSeconds) return 0;
    return Math.min((this.progress.watchedSeconds / this.progress.durationSeconds) * 100, 100);
  }
}
