import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserSettings } from '../models/settings.model';
import { APP_API_BASE_URL } from '../tokens/app-config.token';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(APP_API_BASE_URL);

  getSettings(): Observable<UserSettings> {
    return this.http.get<UserSettings>(`${this.base}/api/settings`);
  }

  saveSettings(settings: UserSettings): Observable<void> {
    return this.http.post<void>(`${this.base}/api/settings`, settings);
  }
}

