import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SettingsService } from '../../core/services/settings.service';
import { UnitsService } from '../../core/services/units.service';
import { UnitPreference, UserSettings } from '../../core/models/settings.model';
import {
  ResortConditionsUpdatedEvent,
  SignalrResortUpdatesService
} from '../../core/services/signalr-resort-updates.service';
import { Subscription } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(SettingsService);
  private readonly unitsService = inject(UnitsService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  private updatesSub?: Subscription;
  subscribedLastViewedResortId?: number;
  lastLiveUpdateAt?: string;

  loading = true;
  saving = false;
  error?: string;

  form = this.fb.nonNullable.group({
    unitPreference: ['imperial' as UnitPreference, Validators.required],
    regionFilter: [''],
    lastViewedResortId: ['']
  });

  ngOnInit(): void {
    this.updatesSub = this.signalrUpdates.updates$.subscribe((evt) =>
      this.handleLiveUpdate(evt)
    );
    this.settingsService.getSettings().subscribe({
      next: (settings) => {
        this.unitsService.setPreference(settings.unitPreference);
        this.patchForm(settings);
        this.loading = false;
        this.syncSignalrForLastViewed(settings);
      },
      error: () => {
        // It is fine if settings do not exist yet
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.updatesSub?.unsubscribe();
    if (this.subscribedLastViewedResortId != null) {
      this.signalrUpdates.unsubscribeFromResort(
        this.subscribedLastViewedResortId.toString()
      );
    }
  }
  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: UserSettings = {
      unitPreference: value.unitPreference,
      regionFilter: value.regionFilter || undefined,
      lastViewedResortId: value.lastViewedResortId
        ? Number(value.lastViewedResortId)
        : undefined
    };

    this.saving = true;
    this.error = undefined;

    this.settingsService.saveSettings(payload).subscribe({
      next: () => {
        this.saving = false;
        this.unitsService.setPreference(payload.unitPreference);
        this.syncSignalrForLastViewed(payload);
        this.snackBar.open('Settings saved', undefined, { duration: 2000 });
      },
      error: () => {
        this.error = 'Unable to save settings.';
        this.saving = false;
      }
    });
  }

  private patchForm(settings: UserSettings): void {
    this.form.patchValue({
      unitPreference: settings.unitPreference,
      regionFilter: settings.regionFilter ?? '',
      lastViewedResortId: settings.lastViewedResortId?.toString() ?? ''
    });
  }
  private syncSignalrForLastViewed(settings: UserSettings): void {
    const nextId = settings.lastViewedResortId;

    if (this.subscribedLastViewedResortId === nextId) {
      return;
    }

    if (this.subscribedLastViewedResortId != null) {
      this.signalrUpdates.unsubscribeFromResort(
        this.subscribedLastViewedResortId.toString()
      );
    }

    this.subscribedLastViewedResortId = nextId;
    this.lastLiveUpdateAt = undefined;

    if (this.subscribedLastViewedResortId != null) {
      this.signalrUpdates.subscribeToResort(
        this.subscribedLastViewedResortId.toString()
      );
    }
  }

  private handleLiveUpdate(evt: ResortConditionsUpdatedEvent): void {
    if (this.subscribedLastViewedResortId == null) {
      return;
    }

    if (evt.resortId !== this.subscribedLastViewedResortId.toString()) {
      return;
    }

    this.lastLiveUpdateAt = new Date().toISOString();
  }
}

