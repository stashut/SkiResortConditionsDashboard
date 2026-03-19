import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Resort, SnowComparisonRow } from '../../core/models/resort.model';
import { ResortService } from '../../core/services/resort.service';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-comparison',
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './comparison.component.html',
  styleUrl: './comparison.component.scss'
})
export class ComparisonComponent implements OnInit, OnDestroy {
  private readonly resortService = inject(ResortService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  private updatesSub?: Subscription;
  private subscribedResortIds = new Set<string>();
  private lastComparedResortIds: string[] = [];
  private refreshInFlight = false;
  private refreshRequested = false;

  allResorts: Resort[] = [];
  selectedIds: string[] = [];

  loadingResorts = true;
  loadingComparison = false;
  error?: string;

  comparisonRows: SnowComparisonRow[] = [];

  readonly displayedColumns = [
    'resortName',
    'observedAt',
    'snowDepth',
    'newSnow'
  ];

  ngOnInit(): void {
    this.resortService.getResorts().subscribe({
      next: (resorts) => {
        this.allResorts = resorts;
        this.loadingResorts = false;
      },
      error: () => {
        this.error = 'Unable to load resorts.';
        this.loadingResorts = false;
      }
    });
    // Keep the comparison fresh if a resort in the active comparison changes.
    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
  }

  ngOnDestroy(): void {
    this.updatesSub?.unsubscribe();
    for (const id of this.subscribedResortIds) {
      this.signalrUpdates.unsubscribeFromResort(id);
    }
    this.subscribedResortIds.clear();
  }

  canCompare(): boolean {
    return this.selectedIds.length > 0 && this.selectedIds.length <= 3;
  }

  private handleLiveUpdate(evt: ResortConditionsUpdatedEvent): void {
    if (this.subscribedResortIds.size === 0) {
      return;
    }

    if (!this.subscribedResortIds.has(evt.resortId)) {
      return;
    }

    // Avoid any flicker if the backend is sending frequent updates.
    // We treat each event as a "refresh needed".
    this.refreshComparison();
  }

  private syncSignalrSubscriptions(resortIds: string[]): void {
    const nextIds = new Set(resortIds);

    for (const id of this.subscribedResortIds) {
      if (!nextIds.has(id)) {
        this.signalrUpdates.unsubscribeFromResort(id);
      }
    }

    for (const id of nextIds) {
      if (!this.subscribedResortIds.has(id)) {
        this.signalrUpdates.subscribeToResort(id);
      }
    }

    this.subscribedResortIds = nextIds;
  }

  private refreshComparison(): void {
    if (this.lastComparedResortIds.length === 0) {
      return;
    }

    if (this.refreshInFlight) {
      this.refreshRequested = true;
      return;
    }

    const ids = [...this.lastComparedResortIds];
    this.refreshInFlight = true;
    this.loadingComparison = true;
    this.error = undefined;

    this.resortService.getSnowComparison(ids).subscribe({
      next: (rows) => {
        this.comparisonRows = rows;
        this.finishRefresh();
      },
      error: () => {
        this.error = 'Unable to load comparison.';
        this.finishRefresh();
      }
    });
  }

  private finishRefresh(): void {
    this.loadingComparison = false;
    this.refreshInFlight = false;

    if (this.refreshRequested) {
      this.refreshRequested = false;
      this.refreshComparison();
    }
  }
  compare(): void {
    if (!this.canCompare()) {
      return;
    }

    this.loadingComparison = true;
    this.error = undefined;

    this.resortService.getSnowComparison(this.selectedIds).subscribe({
      next: (rows) => {
        this.comparisonRows = rows;
        this.loadingComparison = false;
        this.lastComparedResortIds = [...this.selectedIds];
        this.syncSignalrSubscriptions(this.lastComparedResortIds);
      },
      error: () => {
        this.error = 'Unable to load comparison.';
        this.loadingComparison = false;
      }
    });
  }
}

