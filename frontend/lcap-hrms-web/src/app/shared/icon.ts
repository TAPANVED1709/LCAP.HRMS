import { Component, input } from '@angular/core';
@Component({
  selector: 'app-icon',
  template: `<svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.65"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path [attr.d]="paths[name()] || paths['building']" />
  </svg>`,
  styles: [
    `
      :host {
        display: inline-flex;
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }
      svg {
        width: 100%;
        height: 100%;
      }
    `,
  ],
})
export class Icon {
  name = input('building');
  paths: Record<string, string> = {
    building: 'M4 21V3h12v18M2 21h20M16 9h4v12M7 7h1m3 0h1M7 11h1m3 0h1M7 15h1m3 0h1M9 21v-3h3v3',
    dashboard: 'M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z',
    people:
      'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75',
    calendar: 'M4 5h16v16H4zM8 3v4m8-4v4M4 11h16M8 15h2m4 0h2M8 18h2',
    wallet: 'M3 5h16v4M3 5v15h18V9H3M16 13h5v4h-5z',
    file: 'M5 3h10l4 4v14H5zM14 3v5h5M8 12h8m-8 4h6',
    chart: 'M4 3v18h17M8 17v-5m5 5V7m5 10V4',
    settings:
      'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8M9 3h6l1 3 3 1 2 5-2 5-3 1-1 3H9l-1-3-3-1-2-5 2-5 3-1z',
    bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4',
    chevron: 'm9 5 7 7-7 7',
    down: 'm6 9 6 6 6-6',
    plus: 'M12 5v14M5 12h14',
    search: 'M21 21l-5-5M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14',
    edit: 'm4 15 11-11 5 5L9 20H4zM13 6l5 5',
    trash: 'M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7',
    close: 'm6 6 12 12M6 18 18 6',
    check: 'm5 12 4 4L19 6',
    refresh: 'M20 7a8 8 0 1 0 1 8M20 3v5h-5',
    branch: 'M12 3v7M5 21v-6h14v6M12 10v11M9 3h6v5H9z',
    hierarchy: 'M9 3h6v5H9zM12 8v5M4 13h16M4 13v4m8-4v4m8-4v4M2 17h4v4H2zm8 0h4v4h-4zm8 0h4v4h-4z',
    badge: 'M8 3h8v4l-4 3-4-3zM12 10a6 6 0 1 0 0 12 6 6 0 0 0 0-12',
    clock: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18M12 7v5l3 2',
    pin: 'M20 10c0 6-8 11-8 11S4 16 4 10a8 8 0 1 1 16 0M12 7a3 3 0 1 0 0 6 3 3 0 0 0 0-6',
    shield: 'm12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6zM8 12l3 3 5-6',
    menu: 'M4 6h16M4 12h16M4 18h16',
    info: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18M12 11v6m0-10v1',
  };
}
