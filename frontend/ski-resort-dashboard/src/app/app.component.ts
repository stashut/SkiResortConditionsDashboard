import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgForOf, NgIf } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { SignalrResortUpdatesService } from './core/services/signalr-resort-updates.service';
import { ThemeService } from './core/services/theme.service';
import { UnitsService } from './core/services/units.service';
import { SettingsDialogComponent } from './features/settings/settings-dialog.component';

interface NavLink {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NgForOf,
    NgIf,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatDividerModule,
    MatDialogModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);
  private readonly dialog = inject(MatDialog);
  // Inject ThemeService here so its constructor runs at app startup,
  // reading localStorage and applying the saved theme to <html>.
  readonly themeService = inject(ThemeService);
  private readonly unitsService = inject(UnitsService);

  title = 'Ski Resort Conditions';

  navLinks: NavLink[] = [
    { path: '/resorts', label: 'Resorts', icon: 'terrain' },
    { path: '/favorites', label: 'Favorites', icon: 'favorite' },
    { path: '/comparison', label: 'Comparison', icon: 'stacked_bar_chart' }
  ];

  ngOnInit(): void {
    this.signalrUpdates.start();
    this.unitsService.load();
  }

  openSettings(): void {
    this.dialog.open(SettingsDialogComponent, {
      panelClass: 'dark-dialog',
      backdropClass: 'dark-dialog-backdrop',
      autoFocus: false
    });
  }
}
