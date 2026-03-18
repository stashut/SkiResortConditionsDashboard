import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgForOf } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { SignalrResortUpdatesService } from './core/services/signalr-resort-updates.service';

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
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatDividerModule
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly signalrUpdates = inject(SignalrResortUpdatesService);

  title = 'Ski Resort Conditions';

  navLinks: NavLink[] = [
    { path: '/resorts', label: 'Resorts', icon: 'terrain' },
    { path: '/favorites', label: 'Favorites', icon: 'favorite' },
    { path: '/comparison', label: 'Comparison', icon: 'stacked_bar_chart' },
    { path: '/settings', label: 'Settings', icon: 'settings' }
  ];

  ngOnInit(): void {
    this.signalrUpdates.start();
  }
}
