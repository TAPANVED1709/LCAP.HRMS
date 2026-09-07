import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: 'attendance',
    loadComponent: () => import('./attendance/attendance').then((m) => m.Attendance),
  },
  ...['employees', 'employees/new', 'employees/my-team', 'employees/:id/edit', 'employees/:id'].map(
    (path) => ({
      path,
      loadComponent: () => import('./employees/employees').then((m) => m.Employees),
    }),
  ),
  {
    path: 'settings/organisation/:master',
    loadComponent: () => import('./organisation/organisation').then((m) => m.Organisation),
  },
  { path: '', pathMatch: 'full', redirectTo: 'settings/organisation/companies' },
  { path: '**', redirectTo: 'settings/organisation/companies' },
];
