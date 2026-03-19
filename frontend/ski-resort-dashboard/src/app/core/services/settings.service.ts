import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserSettings } from '../models/settings.model';

const API_BASE_URL = '/api';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly http = inject(HttpClient);

  getSettings(): Observable<UserSettings> {
    return this.http.get<UserSettings>(`${API_BASE_URL}/settings`);
  }

  saveSettings(settings: UserSettings): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/settings`, settings);
  }
}

