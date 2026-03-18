import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AddFavoriteRequest, UserFavoriteDto } from '../models/favorites.model';

const API_BASE_URL = '/api';

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly http = inject(HttpClient);

  getFavorites(): Observable<UserFavoriteDto[]> {
    return this.http.get<UserFavoriteDto[]>(`${API_BASE_URL}/favorites`);
  }

  addFavorite(resortId: string): Observable<void> {
    const request: AddFavoriteRequest = { resortId };
    return this.http.post<void>(`${API_BASE_URL}/favorites`, request);
  }

  removeFavorite(resortId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/favorites/${resortId}`);
  }
}

