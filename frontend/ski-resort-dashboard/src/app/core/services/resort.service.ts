import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Resort, SnowComparisonRow } from '../models/resort.model';
import { ResortConditionsResponse } from '../models/condition.model';
import { APP_API_BASE_URL } from '../tokens/app-config.token';

@Injectable({
  providedIn: 'root'
})
export class ResortService {
  private readonly http = inject(HttpClient);
  private readonly base = inject(APP_API_BASE_URL);

  getResorts(): Observable<Resort[]> {
    return this.http.get<Resort[]>(`${this.base}/api/resorts`);
  }

  getResortConditions(id: string): Observable<ResortConditionsResponse> {
    return this.http.get<ResortConditionsResponse>(
      `${this.base}/api/resorts/${id}/conditions`
    );
  }

  getSnowComparison(resortIds: string[]): Observable<SnowComparisonRow[]> {
    // ASP.NET binds Guid[] from repeated query string keys:
    // /api/reports/snow-comparison?resortIds=a&resortIds=b
    let params = new HttpParams();
    for (const id of resortIds) {
      params = params.append('resortIds', id);
    }

    return this.http.get<SnowComparisonRow[]>(
      `${this.base}/api/reports/snow-comparison`,
      { params }
    );
  }
}

