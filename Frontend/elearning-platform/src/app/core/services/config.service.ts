import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config: { apiUrl: string } = { apiUrl: '' };

  get apiUrl(): string {
    return this.config.apiUrl;
  }

  load(): Promise<void> {
    return fetch('/assets/config.json')
      .then(r => r.json())
      .then(json => { this.config = json; });
  }
}
