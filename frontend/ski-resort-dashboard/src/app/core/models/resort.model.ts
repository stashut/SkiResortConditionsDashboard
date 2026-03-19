export interface Resort {
  id: string;
  name: string;
  region: string;
  country?: string;
  elevationBaseMeters?: number;
  elevationTopMeters?: number;
}

export interface SnowComparisonRow {
  resortId: string;
  resortName: string;
  observedAt: string;
  snowDepthCm: number;
  newSnowCm: number;
}

