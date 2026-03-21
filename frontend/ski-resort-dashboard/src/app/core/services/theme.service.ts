import { Injectable, signal, computed } from '@angular/core';

export type Theme = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'ski-theme';
  private readonly _theme = signal<Theme>('dark');

  readonly theme = this._theme.asReadonly();
  readonly isDark = computed(() => this._theme() === 'dark');

  constructor() {
    const saved = localStorage.getItem(this.STORAGE_KEY) as Theme | null;
    this.apply(saved ?? 'dark');
  }

  set(theme: Theme): void {
    this.apply(theme);
    localStorage.setItem(this.STORAGE_KEY, theme);
  }

  toggle(): void {
    this.set(this._theme() === 'dark' ? 'light' : 'dark');
  }

  private apply(theme: Theme): void {
    this._theme.set(theme);
    document.documentElement.setAttribute('data-theme', theme);
  }
}
