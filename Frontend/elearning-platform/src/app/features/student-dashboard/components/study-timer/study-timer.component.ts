import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

type TimerMode = 'focus' | 'short' | 'long' | 'custom';

const MODES: Record<TimerMode, { label: string; minutes: number; color: string }> = {
  focus:  { label: 'Focus',       minutes: 25, color: 'indigo' },
  short:  { label: 'Short Break', minutes: 5,  color: 'green'  },
  long:   { label: 'Long Break',  minutes: 15, color: 'blue'   },
  custom: { label: 'Custom',      minutes: 0,  color: 'purple' }
};

@Component({
  selector: 'app-study-timer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './study-timer.component.html'
})
export class StudyTimerComponent implements OnInit, OnDestroy {
  mode: TimerMode = 'focus';
  secondsLeft = 0;
  totalSeconds = 0;
  running = false;
  customMinutes = 30;
  todaySessions = 0;
  dailyGoal = 4;
  private interval: any;
  private audioCtx: AudioContext | null = null;

  get modeConfig() { return MODES[this.mode]; }
  get minutes(): string { return String(Math.floor(this.secondsLeft / 60)).padStart(2, '0'); }
  get seconds(): string { return String(this.secondsLeft % 60).padStart(2, '0'); }
  get progress(): number { return this.totalSeconds > 0 ? ((this.totalSeconds - this.secondsLeft) / this.totalSeconds) * 100 : 0; }
  get circumference(): number { return 2 * Math.PI * 90; }
  get dashOffset(): number { return this.circumference * (1 - this.progress / 100); }

  ngOnInit(): void {
    const stored = localStorage.getItem('study_timer_data');
    if (stored) {
      try {
        const data = JSON.parse(stored);
        const today = new Date().toDateString();
        if (data.date === today) {
          this.todaySessions = data.sessions || 0;
        }
        this.dailyGoal = data.goal || 4;
      } catch {}
    }
    this.resetTimer();
  }

  ngOnDestroy(): void {
    this.clearInterval();
  }

  setMode(mode: string): void {
    this.mode = mode as TimerMode;
    this.running = false;
    this.clearInterval();
    this.resetTimer();
  }

  resetTimer(): void {
    this.running = false;
    this.clearInterval();
    const mins = this.mode === 'custom' ? this.customMinutes : MODES[this.mode].minutes;
    this.secondsLeft = mins * 60;
    this.totalSeconds = this.secondsLeft;
  }

  toggleTimer(): void {
    if (this.running) {
      this.pause();
    } else {
      this.start();
    }
  }

  private start(): void {
    if (this.secondsLeft === 0) this.resetTimer();
    this.running = true;
    this.interval = setInterval(() => {
      this.secondsLeft--;
      if (this.secondsLeft <= 0) {
        this.secondsLeft = 0;
        this.running = false;
        this.clearInterval();
        if (this.mode === 'focus') {
          this.todaySessions++;
          this.persistData();
        }
        this.playBeep();
      }
    }, 1000);
  }

  private pause(): void {
    this.running = false;
    this.clearInterval();
  }

  private clearInterval(): void {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = null;
    }
  }

  private persistData(): void {
    localStorage.setItem('study_timer_data', JSON.stringify({
      date: new Date().toDateString(),
      sessions: this.todaySessions,
      goal: this.dailyGoal
    }));
  }

  setGoal(goal: number): void {
    this.dailyGoal = goal;
    this.persistData();
  }

  private playBeep(): void {
    try {
      if (!this.audioCtx) {
        this.audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
      }
      const osc = this.audioCtx.createOscillator();
      const gain = this.audioCtx.createGain();
      osc.connect(gain);
      gain.connect(this.audioCtx.destination);
      osc.frequency.value = 660;
      osc.type = 'sine';
      gain.gain.setValueAtTime(0.3, this.audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, this.audioCtx.currentTime + 1.2);
      osc.start(this.audioCtx.currentTime);
      osc.stop(this.audioCtx.currentTime + 1.2);
    } catch {}
  }
}
