import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: 'settings/organisation/:master',
    loadComponent: () => import('./organisation/organisation').then((m) => m.Organisation),
  },
  { path: '', pathMatch: 'full', redirectTo: 'settings/organisation/companies' },
  { path: '**', redirectTo: 'settings/organisation/companies' },
];
