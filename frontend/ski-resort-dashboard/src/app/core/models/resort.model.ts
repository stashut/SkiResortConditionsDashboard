export interface Resort {
  id: number;
  name: string;
  region: string;
  country?: string;
  baseElevationMeters?: number;
  summitElevationMeters?: number;
  runsOpen?: number;
  runsTotal?: number;
  liftsOpen?: number;
  liftsTotal?: number;
}

export interface SnowComparisonRow {
  resortId: number;
  resortName: string;
  baseDepthCm: number;
  newSnow24hCm: number;
  newSnow72hCm: number;
}

