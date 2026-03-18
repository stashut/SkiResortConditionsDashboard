export interface SnowCondition {
  id: string;
  resortId: string;
  observedAt: string;
  snowDepthCm: number;
  newSnowCm: number;
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

