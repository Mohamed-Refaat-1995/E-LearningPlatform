import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config: { apiUrl: string } = { apiUrl: '' };
  platformName = 'U.Learn';
  platformTagLine = 'Learn without limits';

  get apiUrl(): string {
    return this.config.apiUrl;
  }

  get hubUrl(): string {
    return this.config.apiUrl.replace(/\/api\/?$/, '');
  }

  load(): Promise<void> {
    return fetch('/assets/config.json')
      .then(r => r.json())
      .then(json => {
        this.config = json;
        return fetch(`${json.apiUrl}/config/platform`)
          .then(r => r.json())
          .then(p => {
            this.platformName    = p.name    || 'U.Learn';
            this.platformTagLine = p.tagLine || 'Learn without limits';
          })
          .catch(() => { /* backend unavailable — keep defaults */ });
      });
  }
}
