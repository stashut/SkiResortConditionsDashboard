import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Resort } from '../../core/models/resort.model';
import {
  ResortConditionsResponse,
  LiftStatus,
  RunStatus,
  SnowCondition
} from '../../core/models/condition.model';
import { ResortService } from '../../core/services/resort.service';
import {
  SignalrResortUpdatesService,
  ResortConditionsUpdatedEvent
} from '../../core/services/signalr-resort-updates.service';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-resort-detail',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './resort-detail.component.html',
  styleUrl: './resort-detail.component.scss'
})
export class ResortDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly resortService = inject(ResortService);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  resort?: Resort;
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
}

