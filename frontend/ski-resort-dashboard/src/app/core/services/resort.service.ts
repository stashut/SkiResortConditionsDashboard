import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Resort, SnowComparisonRow } from '../models/resort.model';
import { ResortConditionsResponse } from '../models/condition.model';

const API_BASE_URL = '/api';

@Injectable({
  providedIn: 'root'
})
export class ResortService {
  private readonly http = inject(HttpClient);

  getResorts(): Observable<Resort[]> {
    return this.http.get<Resort[]>(`${API_BASE_URL}/resorts`);
  }

  getResortConditions(id: number): Observable<ResortConditionsResponse> {
    return this.http.get<ResortConditionsResponse>(
      `${API_BASE_URL}/resorts/${id}/conditions`
    );
  }

  getSnowComparison(resortIds: number[]): Observable<SnowComparisonRow[]> {
    const params = new HttpParams().set(
      'resortIds',
      resortIds.map((x) => x.toString()).join(',')
    );

    return this.http.get<SnowComparisonRow[]>(
      `${API_BASE_URL}/reports/snow-comparison`,
      { params }
    );
  }
}

