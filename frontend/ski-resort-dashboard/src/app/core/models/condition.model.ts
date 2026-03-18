export interface SnowCondition {
  id: number;
  resortId: number;
  reportedAtUtc: string;
  baseDepthCm: number;
  newSnow24hCm: number;
  temperatureC?: number;
  windSpeedKph?: number;
  runsOpen?: number;
  runsTotal?: number;
  liftsOpen?: number;
  liftsTotal?: number;
  notes?: string;
}

export interface ResortConditionsResponse {
  resort: import('./resort.model').Resort;
  latestCondition: SnowCondition | null;
  recentConditions: SnowCondition[];
}

