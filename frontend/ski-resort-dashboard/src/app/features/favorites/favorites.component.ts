import { Component, OnInit, inject } from '@angular/core';
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
export class FavoritesComponent implements OnInit {
  private readonly favoritesService = inject(FavoritesService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  favorites: FavoriteItem[] = [];
  loading = true;
  error?: string;

  ngOnInit(): void {
    this.loadFavorites();

    this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
  }

  removeFavorite(resort: FavoriteItem): void {
    this.favoritesService.removeFavorite(resort.id).subscribe({
      next: () => {
        this.favorites = this.favorites.filter((f) => f.id !== resort.id);
      },
      error: () => {
        this.error = 'Failed to remove favorite.';
      }
    });
  }

  private loadFavorites(): void {
    this.loading = true;
    this.error = undefined;

    this.favoritesService.getFavorites().subscribe({
      next: (favorites) => {
        this.favorites = favorites;
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to load favorites.';
        this.loading = false;
      }
    });
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

