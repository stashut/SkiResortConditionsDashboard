import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Resort } from '../models/resort.model';

const API_BASE_URL = '/api';

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly http = inject(HttpClient);

  getFavorites(): Observable<Resort[]> {
    return this.http.get<Resort[]>(`${API_BASE_URL}/favorites`);
  }

  addFavorite(resortId: number): Observable<void> {
    return this.http.post<void>(`${API_BASE_URL}/favorites`, { resortId });
  }

  removeFavorite(resortId: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/favorites/${resortId}`);
  }
}

