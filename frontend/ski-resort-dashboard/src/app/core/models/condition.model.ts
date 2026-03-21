export interface SnowCondition {
  id: string;
  resortId: string;
  observedAt: string;
  snowDepthCm: number;
  newSnowCm: number;
  temperatureCelsius?: number | null;
  apparentTemperatureCelsius?: number | null;
  relativeHumidityPercent?: number | null;
  precipitationMm?: number | null;
  rainMm?: number | null;
  weatherCode?: number | null;
  cloudCoverPercent?: number | null;
  windSpeedKmh?: number | null;
  windDirectionDeg?: number | null;
  windGustsKmh?: number | null;
  visibilityMeters?: number | null;
  surfacePressureHpa?: number | null;
}

export interface LiftStatus {
  id: string;
  resortId: string;
  name: string;
  isOpen: boolean;
  updatedAt: string;
}

export interface RunStatus {
  id: string;
  resortId: string;
  name: string;
  isOpen: boolean;
  updatedAt: string;
}

export interface RunStatusPage {
  items: RunStatus[];
  nextUpdatedBefore?: string | null;
  nextIdBefore?: string | null;
}

export interface ResortConditionsResponse {
  resort: import('./resort.model').Resort;
  latestSnowCondition: SnowCondition | null;
  currentLiftStatuses: LiftStatus[];
  runStatusPage: RunStatusPage;
}
