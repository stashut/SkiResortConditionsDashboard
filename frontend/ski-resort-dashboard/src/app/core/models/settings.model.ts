export type UnitPreference = 'imperial' | 'metric';

export interface UserSettings {
  unitPreference: UnitPreference;
  regionFilter?: string;
  lastViewedResortId?: number;
}

