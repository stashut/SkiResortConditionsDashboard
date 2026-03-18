import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Resort } from '../../core/models/resort.model';
import { ResortService } from '../../core/services/resort.service';
import { FavoritesService } from '../../core/services/favorites.service';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { Subscription } from 'rxjs';

interface ResortListItem extends Resort {
  isFavorite?: boolean;
  hasLiveUpdate?: boolean;
}

@Component({
  standalone: true,
  selector: 'app-resort-list',
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  templateUrl: './resort-list.component.html',
  styleUrl: './resort-list.component.scss'
})
export class ResortListComponent implements OnInit, OnDestroy {
  private readonly resortService = inject(ResortService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = [
    'name',
    'region',
    'runs',
    'lifts',
    'favorite',
    'actions'
  ];

  resorts: ResortListItem[] = [];
  loading = true;
  error?: string;

  private updatesSub?: Subscription;

  ngOnInit(): void {
    this.loadData();
    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
  }

  ngOnDestroy(): void {
    this.updatesSub?.unsubscribe();
  }

  navigateTo(resort: Resort): void {
    void this.router.navigate(['/resorts', resort.id]);
  }

  toggleFavorite(resort: ResortListItem): void {
    if (resort.isFavorite) {
      this.favoritesService.removeFavorite(resort.id).subscribe({
        next: () => {
          resort.isFavorite = false;
          this.snackBar.open(`Removed ${resort.name} from favorites`, undefined, {
            duration: 2500
          });
        },
        error: () => {
          this.snackBar.open('Failed to update favorite', undefined, {
            duration: 2500
          });
        }
      });
    } else {
      this.favoritesService.addFavorite(resort.id).subscribe({
        next: () => {
          resort.isFavorite = true;
          this.snackBar.open(`Added ${resort.name} to favorites`, undefined, {
            duration: 2500
          });
        },
        error: () => {
          this.snackBar.open('Failed to update favorite', undefined, {
            duration: 2500
          });
        }
      });
    }
  }

  private loadData(): void {
    this.loading = true;
    this.error = undefined;

    this.resortService.getResorts().subscribe({
      next: (resorts) => {
        this.resorts = resorts;
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to load resorts right now.';
        this.loading = false;
      }
    });

    this.favoritesService.getFavorites().subscribe({
      next: (favorites) => {
        const favoriteIds = new Set(favorites.map((f) => f.id));
        this.resorts = this.resorts.map((r) => ({
          ...r,
          isFavorite: favoriteIds.has(r.id)
        }));
      },
      error: () => {
        // Non-fatal; favorites will just not be pre-populated
      }
    });
  }

  private handleLiveUpdate(evt: ResortConditionsUpdatedEvent): void {
    const target = this.resorts.find((r) => r.id === evt.resortId);
    if (!target) {
      return;
    }

    target.hasLiveUpdate = true;
    setTimeout(() => {
      target.hasLiveUpdate = false;
    }, 3000);
  }
}

