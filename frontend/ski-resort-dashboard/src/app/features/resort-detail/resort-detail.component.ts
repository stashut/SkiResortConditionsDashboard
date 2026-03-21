import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Resort } from '../../core/models/resort.model';
import {
  ResortConditionsResponse,
  LiftStatus,
  RunStatus,
  SnowCondition
} from '../../core/models/condition.model';
import { ResortService } from '../../core/services/resort.service';
import { FavoritesService } from '../../core/services/favorites.service';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { Subscription } from 'rxjs';
import {
  fmtNum,
  formatWindDirection,
  getOpenMeteoWeatherLabel
} from '../../core/utils/open-meteo-weather.util';
import { UnitsService } from '../../core/services/units.service';

@Component({
  standalone: true,
  selector: 'app-resort-detail',
  imports: [
    CommonModule,
    DatePipe,
    RouterLink,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  templateUrl: './resort-detail.component.html',
  styleUrl: './resort-detail.component.scss'
})
export class ResortDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly resortService = inject(ResortService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);
  private readonly snackBar = inject(MatSnackBar);
  readonly units = inject(UnitsService);

  resort?: Resort;
  /** Whether this resort is in the user's favorites (from API). */
  isFavorite = false;
  latestSnowCondition?: SnowCondition | null;
  currentLiftStatuses: LiftStatus[] = [];
  recentRunStatuses: RunStatus[] = [];

  loading = true;
  error?: string;

  private updatesSub?: Subscription;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'Invalid resort id.';
      this.loading = false;
      return;
    }

    this.loadResortConditions(id);
    this.signalrUpdates.subscribeToResort(id);
    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(id, evt)
    );
  }

  ngOnDestroy(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.signalrUpdates.unsubscribeFromResort(id);
    }

    this.updatesSub?.unsubscribe();
  }

  private loadResortConditions(id: string): void {
    this.loading = true;
    this.error = undefined;

    this.resortService.getResortConditions(id).subscribe({
      next: (response: ResortConditionsResponse) => {
        this.resort = response.resort;
        this.latestSnowCondition = response.latestSnowCondition;
        this.currentLiftStatuses = response.currentLiftStatuses;
        this.recentRunStatuses = response.runStatusPage.items;
        this.loading = false;
        this.refreshFavoriteState(id);
      },
      error: () => {
        this.error = 'Unable to load resort conditions.';
        this.loading = false;
      }
    });
  }

  private handleLiveUpdate(
    currentResortId: string,
    evt: ResortConditionsUpdatedEvent
  ): void {
    if (evt.resortId !== currentResortId) {
      return;
    }

    // For now, just trigger a reload; you can later project payload shape directly.
    this.loadResortConditions(currentResortId);
  }

  toggleFavorite(): void {
    const id = this.resort?.id;
    const name = this.resort?.name ?? 'Resort';
    if (!id) return;

    if (this.isFavorite) {
      this.favoritesService.removeFavorite(id).subscribe({
        next: () => {
          this.isFavorite = false;
          this.snackBar.open(`Removed ${name} from favorites`, undefined, {
            duration: 2500
          });
        },
        error: () =>
          this.snackBar.open('Failed to update favorite', undefined, {
            duration: 2500
          })
      });
    } else {
      this.favoritesService.addFavorite(id).subscribe({
        next: () => {
          this.isFavorite = true;
          this.snackBar.open(`Added ${name} to favorites`, undefined, {
            duration: 2500
          });
        },
        error: () =>
          this.snackBar.open('Failed to update favorite', undefined, {
            duration: 2500
          })
      });
    }
  }

  private refreshFavoriteState(resortId: string): void {
    this.favoritesService.getFavorites().subscribe({
      next: (favs) => {
        this.isFavorite = favs.some((f) => f.resortId === resortId);
      },
      error: () => {}
    });
  }

  weatherLabel(code: number | null | undefined): string {
    return getOpenMeteoWeatherLabel(code);
  }

  windDir(deg: number | null | undefined): string {
    return formatWindDirection(deg);
  }

  fmt(v: number | null | undefined, suffix = '', digits = 1): string {
    return fmtNum(v, suffix, digits);
  }
}

