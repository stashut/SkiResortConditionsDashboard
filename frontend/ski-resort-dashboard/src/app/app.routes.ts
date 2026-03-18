import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'resorts'
  },
  {
    path: 'resorts',
    loadComponent: () =>
      import('./features/resort-list/resort-list.component').then(
        (m) => m.ResortListComponent
      )
  },
  {
    path: 'resorts/:id',
    loadComponent: () =>
      import('./features/resort-detail/resort-detail.component').then(
        (m) => m.ResortDetailComponent
      )
  },
  {
    path: 'favorites',
    loadComponent: () =>
      import('./features/favorites/favorites.component').then(
        (m) => m.FavoritesComponent
      )
  },
  {
    path: 'comparison',
    loadComponent: () =>
      import('./features/comparison/comparison.component').then(
        (m) => m.ComparisonComponent
      )
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./features/settings/settings.component').then(
        (m) => m.SettingsComponent
      )
  },
  {
    path: '**',
    redirectTo: 'resorts'
  }
];
