import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AddFavoriteRequest, UserFavoriteDto } from '../models/favorites.model';
import { APP_API_BASE_URL } from '../tokens/app-config.token';

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(APP_API_BASE_URL);

  getFavorites(): Observable<UserFavoriteDto[]> {
    return this.http.get<UserFavoriteDto[]>(`${this.base}/api/favorites`);
  }

  addFavorite(resortId: string): Observable<void> {
    const request: AddFavoriteRequest = { resortId };
    return this.http.post<void>(`${this.base}/api/favorites`, request);
  }

  removeFavorite(resortId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/favorites/${resortId}`);
  }
}

