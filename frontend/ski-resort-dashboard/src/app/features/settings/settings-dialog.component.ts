import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SettingsService } from '../../core/services/settings.service';
import { ThemeService } from '../../core/services/theme.service';
import { UnitsService } from '../../core/services/units.service';
import { UnitPreference, UserSettings } from '../../core/models/settings.model';

@Component({
  standalone: true,
  selector: 'app-settings-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <div class="sd-container">
      <div class="sd-header">
        <mat-icon class="sd-header__icon">settings</mat-icon>
        <h2 class="sd-header__title">Settings</h2>
        <button mat-icon-button class="sd-close" (click)="close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="sd-body">

        <!-- Appearance -->
        <div class="sd-field">
          <div class="sd-label">Appearance</div>
          <mat-button-toggle-group
            [value]="themeService.theme()"
            (change)="themeService.set($event.value)"
            class="sd-toggle sd-toggle--full">
            <mat-button-toggle value="dark">
              <mat-icon>dark_mode</mat-icon>
              Dark
            </mat-button-toggle>
            <mat-button-toggle value="light">
              <mat-icon>light_mode</mat-icon>
              Light
            </mat-button-toggle>
          </mat-button-toggle-group>
        </div>

        <div class="sd-divider"></div>

        <form [formGroup]="form" (ngSubmit)="save()" class="sd-form">

          <!-- Units -->
          <div class="sd-field">
            <div class="sd-label">Units</div>
            <mat-button-toggle-group formControlName="unitPreference" class="sd-toggle sd-toggle--full">
              <mat-button-toggle value="metric">
                <mat-icon>straighten</mat-icon>
                Metric (cm / m)
              </mat-button-toggle>
              <mat-button-toggle value="imperial">
                <mat-icon>straighten</mat-icon>
                Imperial (in / ft)
              </mat-button-toggle>
            </mat-button-toggle-group>
          </div>

          <!-- Region filter -->
          <div class="sd-field">
            <div class="sd-label">Region filter</div>
            <div class="sd-hint">Only show resorts from a specific region</div>
            <mat-form-field appearance="outline" class="sd-input">
              <mat-label>Region</mat-label>
              <input matInput formControlName="regionFilter"
                placeholder="e.g. Alps, Carpathians, Tatras" />
              <button *ngIf="form.value.regionFilter" matSuffix mat-icon-button
                (click)="form.patchValue({ regionFilter: '' })" type="button">
                <mat-icon>close</mat-icon>
              </button>
            </mat-form-field>
          </div>

          <div class="sd-error" *ngIf="error">{{ error }}</div>

          <div class="sd-actions">
            <button mat-stroked-button type="button" (click)="close()">Cancel</button>
            <button mat-flat-button type="submit" [disabled]="saving" class="sd-save-btn">
              <mat-spinner *ngIf="saving" diameter="16"></mat-spinner>
              <span>Save</span>
            </button>
          </div>

        </form>
      </div>
    </div>
  `,
  styles: [`
    .sd-container {
      background: #0f172a;
      border-radius: 12px;
      overflow: hidden;
      min-width: 360px;
      max-width: 440px;
    }

    .sd-header {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 1.25rem 1.25rem 1rem;
      border-bottom: 1px solid rgba(148, 163, 184, 0.1);

      &__icon { color: rgba(148, 163, 184, 0.6); font-size: 1.2rem; width: 1.2rem; height: 1.2rem; }
      &__title { flex: 1; margin: 0; font-size: 1rem; font-weight: 600; color: #f1f5f9; }
    }

    .sd-close { color: rgba(148, 163, 184, 0.5) !important; }

    .sd-body {
      padding: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 1.1rem;
    }

    .sd-form {
      display: flex;
      flex-direction: column;
      gap: 1.1rem;
    }

    .sd-divider {
      height: 1px;
      background: rgba(148, 163, 184, 0.1);
      margin: 0 -1.25rem;
    }

    .sd-label {
      font-size: 0.72rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: rgba(148, 163, 184, 0.6);
      margin-bottom: 0.5rem;
    }

    .sd-hint {
      font-size: 0.75rem;
      color: rgba(148, 163, 184, 0.45);
      margin-bottom: 0.6rem;
      margin-top: -0.3rem;
    }

    .sd-toggle {
      background: rgba(255,255,255,0.04);
      border-radius: 8px;
      border: 1px solid rgba(148, 163, 184, 0.15);

      &--full { width: 100%; }

      ::ng-deep .mat-button-toggle {
        flex: 1;
        background: transparent;
        color: rgba(148, 163, 184, 0.7);
        border-color: rgba(148, 163, 184, 0.12);

        &.mat-button-toggle-checked {
          background: rgba(34, 211, 238, 0.12);
          color: #e2e8f0;
        }

        .mat-button-toggle-label-content {
          display: flex;
          align-items: center;
          gap: 6px;
          padding: 0 14px;
          line-height: 38px;
          font-size: 0.82rem;
        }

        mat-icon { font-size: 16px; width: 16px; height: 16px; }
      }
    }

    .sd-input {
      width: 100%;

      ::ng-deep {
        .mat-mdc-form-field-flex { background: rgba(255,255,255,0.04); }
        .mdc-notched-outline__leading,
        .mdc-notched-outline__notch,
        .mdc-notched-outline__trailing { border-color: rgba(148, 163, 184, 0.2) !important; }
        .mdc-floating-label { color: rgba(148, 163, 184, 0.6) !important; }
        input.mat-mdc-input-element { color: #e2e8f0 !important; caret-color: #e2e8f0; }
        .mat-mdc-form-field-icon-suffix button { color: rgba(148, 163, 184, 0.5) !important; }
      }
    }

    .sd-error {
      color: #fca5a5;
      font-size: 0.8rem;
      padding: 0.5rem 0.75rem;
      background: rgba(248, 113, 113, 0.08);
      border-radius: 6px;
      border: 1px solid rgba(248, 113, 113, 0.2);
    }

    .sd-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.6rem;
      padding-top: 0.25rem;
      border-top: 1px solid rgba(148, 163, 184, 0.1);

      button[mat-stroked-button] {
        color: rgba(148, 163, 184, 0.7) !important;
        border-color: rgba(148, 163, 184, 0.2) !important;
      }
    }

    .sd-save-btn {
      background: #22d3ee !important;
      color: #0a0f1e !important;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 6px;
    }
  `]
})
export class SettingsDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(SettingsService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<SettingsDialogComponent>);

  readonly themeService = inject(ThemeService);
  private readonly unitsService = inject(UnitsService);

  saving = false;
  error?: string;

  form = this.fb.nonNullable.group({
    unitPreference: ['metric' as UnitPreference, Validators.required],
    regionFilter: ['']
  });

  ngOnInit(): void {
    this.settingsService.getSettings().subscribe({
      next: (s) => {
        this.unitsService.setPreference(s.unitPreference);
        this.form.patchValue({
          unitPreference: s.unitPreference,
          regionFilter: s.regionFilter ?? ''
        });
      },
      error: () => {}
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: UserSettings = {
      unitPreference: v.unitPreference,
      regionFilter: v.regionFilter || undefined
    };

    this.saving = true;
    this.error = undefined;

    this.settingsService.saveSettings(payload).subscribe({
      next: () => {
        this.saving = false;
        this.unitsService.setPreference(payload.unitPreference);
        this.snackBar.open('Settings saved', undefined, { duration: 2000 });
        this.dialogRef.close(payload);
      },
      error: () => {
        this.error = 'Unable to save settings.';
        this.saving = false;
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
