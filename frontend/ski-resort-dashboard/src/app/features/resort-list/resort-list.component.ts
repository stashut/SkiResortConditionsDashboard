import {
  AfterViewInit,
  Component,
  effect,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { Resort } from '../../core/models/resort.model';
import { ResortConditionsResponse, SnowCondition, LiftStatus, RunStatus } from '../../core/models/condition.model';
import { ResortService } from '../../core/services/resort.service';
import { FavoritesService } from '../../core/services/favorites.service';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { forkJoin, retry, Subscription } from 'rxjs';
import * as L from 'leaflet';
import {
  fmtNum,
  formatWindDirection,
  getOpenMeteoWeatherLabel
} from '../../core/utils/open-meteo-weather.util';
import { UnitsService } from '../../core/services/units.service';

interface ResortListItem extends Resort {
  isFavorite?: boolean;
  hasLiveUpdate?: boolean;
  _marker?: L.Marker;
}

@Component({
  standalone: true,
  selector: 'app-resort-list',
  imports: [
    CommonModule,
    DatePipe,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './resort-list.component.html',
  styleUrl: './resort-list.component.scss'
})
export class ResortListComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly resortService = inject(ResortService);
  private readonly favoritesService = inject(FavoritesService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);
  private readonly snackBar = inject(MatSnackBar);
  readonly units = inject(UnitsService);

  constructor() {
    effect(() => {
      this.units.preference();
      if (!this.map) return;
      for (const resort of this.resorts) {
        resort._marker?.setPopupContent(this.buildPopupHtml(resort));
      }
    });
  }

  @ViewChild('mapContainer') mapContainer?: ElementRef<HTMLDivElement>;

  displayedColumns = ['resort', 'actions'];

  resorts: ResortListItem[] = [];
  loading = true;
  error?: string;

  // Detail panel state
  panelMode: 'list' | 'detail' = 'list';
  detailLoading = false;
  detailError?: string;
  detailResort?: ResortListItem;
  detailSnow?: SnowCondition | null;
  detailLifts: LiftStatus[] = [];
  detailRuns: RunStatus[] = [];

  private map?: L.Map;
  private updatesSub?: Subscription;
  private subscribedResortIds = new Set<string>();
  private highlightedMarker?: L.Marker;

  ngOnInit(): void {
    this.loadData();
    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
  }

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    this.updatesSub?.unsubscribe();
    this.map?.remove();
    this.map = undefined;
    for (const id of this.subscribedResortIds) {
      this.signalrUpdates.unsubscribeFromResort(id);
    }
  }

  // ── Panel navigation + map ─────────────────────────────────────────────────

  /** Single entry point: shows detail panel AND pans the map */
  selectResort(resort: ResortListItem): void {
    this.showDetail(resort);
    if (this.map && resort.latitudeDeg != null && resort.longitudeDeg != null) {
      this.map.flyTo([resort.latitudeDeg, resort.longitudeDeg], 11, { duration: 0.9 });
      resort._marker?.openPopup();
    }
  }

  showDetail(resort: ResortListItem): void {
    this.panelMode = 'detail';
    this.detailLoading = true;
    this.detailError = undefined;
    this.detailResort = resort;
    this.detailSnow = undefined;
    this.detailLifts = [];
    this.detailRuns = [];

    this.resortService.getResortConditions(resort.id).subscribe({
      next: (r: ResortConditionsResponse) => {
        // Merge API resort onto the list row so favorite + marker stay in sync
        const row =
          this.resorts.find((x) => x.id === resort.id) ?? resort;
        const fav = row.isFavorite;
        const marker = row._marker;
        Object.assign(row, r.resort);
        row.isFavorite = fav;
        row._marker = marker;
        this.detailResort = row;
        this.detailSnow = r.latestSnowCondition;
        this.detailLifts = r.currentLiftStatuses;
        this.detailRuns = r.runStatusPage.items;
        this.detailLoading = false;
      },
      error: () => {
        this.detailError = 'Unable to load resort conditions.';
        this.detailLoading = false;
      }
    });

    // Also highlight the marker
    this.highlightMarker(resort);
  }

  backToList(): void {
    this.panelMode = 'list';
    if (this.highlightedMarker) {
      this.highlightedMarker.getElement()
        ?.querySelector('.resort-marker')
        ?.classList.remove('resort-marker--highlight');
      this.highlightedMarker = undefined;
    }
  }

  // ── Map interactions ────────────────────────────────────────────────────────
  toggleFavorite(resort: ResortListItem): void {
    if (resort.isFavorite) {
      this.favoritesService.removeFavorite(resort.id).subscribe({
        next: () => {
          resort.isFavorite = false;
          this.snackBar.open(`Removed ${resort.name} from favorites`, undefined, { duration: 2500 });
        },
        error: () => this.snackBar.open('Failed to update favorite', undefined, { duration: 2500 })
      });
    } else {
      this.favoritesService.addFavorite(resort.id).subscribe({
        next: () => {
          resort.isFavorite = true;
          this.snackBar.open(`Added ${resort.name} to favorites`, undefined, { duration: 2500 });
        },
        error: () => this.snackBar.open('Failed to update favorite', undefined, { duration: 2500 })
      });
    }
  }

  // ── Private ────────────────────────────────────────────────────────────────

  private highlightMarker(resort: ResortListItem): void {
    if (this.highlightedMarker) {
      this.highlightedMarker.getElement()
        ?.querySelector('.resort-marker')
        ?.classList.remove('resort-marker--highlight');
    }
    if (resort._marker) {
      resort._marker.getElement()
        ?.querySelector('.resort-marker')
        ?.classList.add('resort-marker--highlight');
      this.highlightedMarker = resort._marker;
    }
  }

  private initMap(): void {
    if (!this.mapContainer) return;

    this.map = L.map(this.mapContainer.nativeElement, {
      center: [52, 15],
      zoom: 5,
      zoomControl: true
    });

    L.tileLayer(
      'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
      {
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> ' +
          'contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
        subdomains: 'abcd',
        maxZoom: 19
      }
    ).addTo(this.map);

    setTimeout(() => {
      this.map?.invalidateSize();
      for (const resort of this.resorts) {
        this.placeMarker(resort);
      }
      // Do NOT fitBounds — that would zoom to world view (US + Japan markers).
      // The initial EU view is set by center/zoom in the map constructor above.
    }, 50);
  }


  private buildPopupHtml(resort: ResortListItem): string {
    const elevLine = resort.elevationTopMeters
      ? `<div class="rp-elev">${this.units.formatElevationPopupSummary(
          resort.elevationBaseMeters,
          resort.elevationTopMeters
        )}</div>`
      : '';
    return `
      <div class="resort-popup">
        <div class="rp-name">${resort.name}</div>
        <div class="rp-region">${resort.region}${resort.country ? ' · ' + resort.country : ''}</div>
        ${elevLine}
        <button class="rp-btn" data-id="${resort.id}">View details →</button>
      </div>`;
  }

  private placeMarker(resort: ResortListItem): void {
    if (!this.map || resort.latitudeDeg == null || resort.longitudeDeg == null || resort._marker) return;

    const icon = L.divIcon({
      className: '',
      html: `<div class="resort-marker"></div>`,
      iconSize: [12, 12],
      iconAnchor: [6, 6],
      popupAnchor: [0, -10]
    });

    const popup = L.popup({ className: 'resort-popup-wrapper', minWidth: 185 }).setContent(
      this.buildPopupHtml(resort)
    );

    const marker = L.marker([resort.latitudeDeg!, resort.longitudeDeg!], { icon })
      .bindPopup(popup)
      .addTo(this.map!);

    // Clicking the marker should zoom in + open detail panel (same as clicking from list)
    marker.on('click', () => {
      this.map?.flyTo([resort.latitudeDeg!, resort.longitudeDeg!], 11, { duration: 0.9 });
      this.highlightMarker(resort);
      this.showDetail(resort);
    });

    marker.on('popupopen', () => {
      const btn = document.querySelector(`.rp-btn[data-id="${resort.id}"]`) as HTMLElement | null;
      btn?.addEventListener('click', () => {
        marker.closePopup();
        this.selectResort(resort);
      }, { once: true });
    });

    resort._marker = marker;
  }

  private loadData(): void {
    this.loading = true;
    this.error = undefined;

    forkJoin({
      resorts: this.resortService.getResorts(),
      favorites: this.favoritesService.getFavorites()
    })
      .pipe(retry({ count: 3, delay: 1500 }))
      .subscribe({
        next: ({ resorts, favorites }) => {
          const ids = new Set(favorites.map((f) => f.resortId));
          this.resorts = resorts.map((r) => ({ ...r, isFavorite: ids.has(r.id) }));
          this.loading = false;
          this.syncSignalrSubscriptions(resorts.map((r) => r.id));
          for (const resort of this.resorts) {
            this.placeMarker(resort);
          }
          const openId = this.detailResort?.id;
          if (this.panelMode === 'detail' && openId) {
            this.detailResort = this.resorts.find((r) => r.id === openId);
          }
        },
        error: (err) => {
          const status = (err as any)?.status;
          this.error = `Unable to load resorts${status ? ` (HTTP ${status})` : ''}.`;
          this.loading = false;
        }
      });
  }

  private syncSignalrSubscriptions(resortIds: string[]): void {
    const next = new Set(resortIds);
    for (const id of this.subscribedResortIds) {
      if (!next.has(id)) this.signalrUpdates.unsubscribeFromResort(id);
    }
    for (const id of next) {
      if (!this.subscribedResortIds.has(id)) this.signalrUpdates.subscribeToResort(id);
    }
    this.subscribedResortIds = next;
  }

  private handleLiveUpdate(evt: ResortConditionsUpdatedEvent): void {
    const target = this.resorts.find((r) => r.id === evt.resortId);
    if (!target) return;
    target.hasLiveUpdate = true;
    setTimeout(() => { target.hasLiveUpdate = false; }, 3000);

    // If this resort is currently open in the detail panel, refresh it
    if (this.panelMode === 'detail' && this.detailResort?.id === evt.resortId) {
      this.resortService.getResortConditions(evt.resortId).subscribe({
        next: (r) => {
          this.detailSnow = r.latestSnowCondition;
          this.detailLifts = r.currentLiftStatuses;
          this.detailRuns = r.runStatusPage.items;
        },
        error: () => {}
      });
    }
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
