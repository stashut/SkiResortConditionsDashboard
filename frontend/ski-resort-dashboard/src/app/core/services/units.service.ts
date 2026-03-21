import { Injectable, inject, signal } from '@angular/core';
import { UnitPreference } from '../models/settings.model';
import { SettingsService } from './settings.service';
import { fmtNum } from '../utils/open-meteo-weather.util';

const CM_PER_IN = 2.54;
const M_TO_FT = 3.28084;
const MM_PER_IN = 25.4;
const KMH_TO_MPH = 0.621371;
const M_TO_MI = 1 / 1609.344;
const HPA_TO_INHG = 0.02953;

function celsiusToFahrenheit(c: number): number {
  return (c * 9) / 5 + 32;
}

@Injectable({ providedIn: 'root' })
export class UnitsService {
  private readonly settingsService = inject(SettingsService);

  /** Current display preference; synced from API on startup and when settings are saved. */
  readonly preference = signal<UnitPreference>('metric');

  /** Load saved preference from backend (call once at app startup). */
  load(): void {
    this.settingsService.getSettings().subscribe({
      next: (s) =>
        this.preference.set(s.unitPreference === 'imperial' ? 'imperial' : 'metric'),
      error: () => this.preference.set('metric')
    });
  }

  setPreference(p: UnitPreference): void {
    this.preference.set(p === 'imperial' ? 'imperial' : 'metric');
  }

  formatSnowCm(value: number | null | undefined): string {
    if (value == null) return '—';
    const p = this.preference();
    if (p === 'metric') {
      const n = Number(value);
      return `${Number.isInteger(n) ? n : n.toFixed(1)} cm`;
    }
    const inches = Number(value) / CM_PER_IN;
    return `${inches.toFixed(1)} in`;
  }

  /** Elevation range for display (base → summit). */
  formatElevationRange(
    baseM: number | null | undefined,
    topM: number | null | undefined
  ): string {
    if (baseM == null || topM == null) return '—';
    const p = this.preference();
    if (p === 'metric') {
      return `${baseM}m → ${topM}m`;
    }
    const b = Math.round(baseM * M_TO_FT);
    const t = Math.round(topM * M_TO_FT);
    return `${b} ft → ${t} ft`;
  }

  /** Short elevation for list rows (summit only). */
  formatElevationTop(topM: number | null | undefined): string {
    if (topM == null) return '';
    const p = this.preference();
    if (p === 'metric') return `${topM}m`;
    return `${Math.round(topM * M_TO_FT)} ft`;
  }

  /** Map popup: base may be unknown (?). */
  formatElevationPopupSummary(
    baseM: number | null | undefined,
    topM: number | null | undefined
  ): string {
    if (topM == null) return '';
    const p = this.preference();
    if (p === 'metric') {
      const b = baseM != null ? `${baseM}m` : '?m';
      return `▲ ${b} – ${topM}m`;
    }
    const t = Math.round(topM * M_TO_FT);
    const b = baseM != null ? `${Math.round(baseM * M_TO_FT)} ft` : '? ft';
    return `▲ ${b} – ${t} ft`;
  }

  formatTempC(value: number | null | undefined): string {
    if (value == null) return '—';
    const p = this.preference();
    if (p === 'metric') return fmtNum(value, ' °C', 1);
    return `${celsiusToFahrenheit(Number(value)).toFixed(1)} °F`;
  }

  formatPrecipMm(value: number | null | undefined, digits = 2): string {
    if (value == null) return '—';
    const p = this.preference();
    if (p === 'metric') return fmtNum(value, ' mm', digits);
    const inches = Number(value) / MM_PER_IN;
    return `${inches.toFixed(digits)} in`;
  }

  formatWindKmh(value: number | null | undefined, digits = 1): string {
    if (value == null) return '—';
    const p = this.preference();
    if (p === 'metric') return fmtNum(value, ' km/h', digits);
    const mph = Number(value) * KMH_TO_MPH;
    return `${mph.toFixed(digits)} mph`;
  }

  formatVisibilityM(value: number | null | undefined): string {
    if (value == null) return '—';
    const m = Number(value);
    const p = this.preference();
    if (p === 'metric') return fmtNum(m, ' m', 0);
    if (m >= 1609.344) {
      return `${(m * M_TO_MI).toFixed(2)} mi`;
    }
    return `${Math.round(m * M_TO_FT)} ft`;
  }

  formatPressureHpa(value: number | null | undefined): string {
    if (value == null) return '—';
    const p = this.preference();
    if (p === 'metric') return fmtNum(value, ' hPa', 0);
    const inHg = Number(value) * HPA_TO_INHG;
    return `${inHg.toFixed(2)} inHg`;
  }

  /** Label for height above ground in weather (2 m vs 6 ft). */
  heightAboveGroundLabel(): string {
    return this.preference() === 'metric' ? 'Temp (2 m)' : 'Temp (6 ft)';
  }
}
