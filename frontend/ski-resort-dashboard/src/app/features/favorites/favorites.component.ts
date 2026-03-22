import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FavoritesService } from '../../core/services/favorites.service';
import { Resort } from '../../core/models/resort.model';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { ResortService } from '../../core/services/resort.service';
import { forkJoin, retry } from 'rxjs';
import { Subscription } from 'rxjs';

interface FavoriteItem extends Resort {
  hasLiveUpdate?: boolean;
}

@Component({
  standalone: true,
  selector: 'app-favorites',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './favorites.component.html',
  styleUrl: './favorites.component.scss'
})
export class FavoritesComponent implements OnInit, OnDestroy {
  private readonly favoritesService = inject(FavoritesService);
  private readonly resortService = inject(ResortService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  favorites: FavoriteItem[] = [];
  loading = true;
  error?: string;

  private updatesSub?: Subscription;
  private subscribedFavoriteResortIds = new Set<string>();

  ngOnInit(): void {
    this.loadFavorites();

    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
  }

  ngOnDestroy(): void {
    this.updatesSub?.unsubscribe();

    for (const id of this.subscribedFavoriteResortIds) {
      this.signalrUpdates.unsubscribeFromResort(id);
    }
    this.subscribedFavoriteResortIds.clear();
  }
  removeFavorite(resort: FavoriteItem): void {
    this.favoritesService.removeFavorite(resort.id).subscribe({
      next: () => {
        this.favorites = this.favorites.filter((f) => f.id !== resort.id);
        this.subscribedFavoriteResortIds.delete(resort.id);
        this.signalrUpdates.unsubscribeFromResort(resort.id);
      },
      error: () => {
        this.error = 'Failed to remove favorite.';
      }
    });
  }

  private loadFavorites(): void {
    this.loading = true;
    this.error = undefined;

    forkJoin({
      resorts: this.resortService.getResorts(),
      favorites: this.favoritesService.getFavorites()
    })
      .pipe(retry({ count: 3, delay: 1500 }))
      .subscribe({
        next: ({ resorts, favorites }) => {
          const favoriteIds = new Set(favorites.map((f) => f.resortId));
          this.favorites = resorts
            .filter((r) => favoriteIds.has(r.id))
            .map((r) => ({ ...r, hasLiveUpdate: false }));
          this.syncSignalrSubscriptions(this.favorites.map((f) => f.id));
          this.loading = false;
        },
        error: () => {
          this.error = 'Unable to load favorites.';
          this.loading = false;
        }
      });
  }

  retryLoad(): void {
    this.loadFavorites();
  }

  private syncSignalrSubscriptions(resortIds: string[]): void {
    const next = new Set(resortIds);

    for (const id of this.subscribedFavoriteResortIds) {
      if (!next.has(id)) {
        this.signalrUpdates.unsubscribeFromResort(id);
      }
    }

    for (const id of next) {
      if (!this.subscribedFavoriteResortIds.has(id)) {
        this.signalrUpdates.subscribeToResort(id);
      }
    }

    this.subscribedFavoriteResortIds = next;
  }
  private handleLiveUpdate(evt: ResortConditionsUpdatedEvent): void {
    const match = this.favorites.find((f) => f.id === evt.resortId);
    if (!match) {
      return;
    }

    match.hasLiveUpdate = true;
    setTimeout(() => (match.hasLiveUpdate = false), 3000);
  }
}

