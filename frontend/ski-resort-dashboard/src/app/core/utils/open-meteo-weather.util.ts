/** WMO weather interpretation codes (Open-Meteo). */
export function getOpenMeteoWeatherLabel(code: number | null | undefined): string {
  if (code == null) return '—';
  const c = Math.round(code);
  if (c === 0) return 'Clear';
  if (c === 1) return 'Mainly clear';
  if (c === 2) return 'Partly cloudy';
  if (c === 3) return 'Overcast';
  if (c === 45 || c === 48) return 'Fog';
  if (c >= 51 && c <= 55) return 'Drizzle';
  if (c >= 56 && c <= 57) return 'Freezing drizzle';
  if (c >= 61 && c <= 65) return 'Rain';
  if (c >= 66 && c <= 67) return 'Freezing rain';
  if (c >= 71 && c <= 75) return 'Snow';
  if (c === 77) return 'Snow grains';
  if (c >= 80 && c <= 82) return 'Rain showers';
  if (c === 85 || c === 86) return 'Snow showers';
  if (c === 95) return 'Thunderstorm';
  if (c === 96 || c === 99) return 'Thunderstorm (hail)';
  return `Code ${c}`;
}

const compass = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'] as const;

export function formatWindDirection(deg: number | null | undefined): string {
  if (deg == null) return '—';
  const d = ((Math.round(deg) % 360) + 360) % 360;
  const idx = Math.round(d / 45) % 8;
  return `${d}° ${compass[idx]}`;
}

export function fmtNum(
  v: number | null | undefined,
  suffix = '',
  digits = 1
): string {
  if (v == null) return '—';
  return `${Number(v).toFixed(digits)}${suffix}`;
}
