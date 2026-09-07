import { Component, input, output, signal, computed, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Icon } from './icon';
import { Master, Row } from '../organisation/master-config';
@Component({
  selector: 'app-data-table',
  imports: [FormsModule, Icon],
  template: `
    <div class="table-toolbar">
      <div class="search-box">
        <app-icon name="search" /><input
          aria-label="Search records"
          placeholder="Search by name, code or details…"
          [ngModel]="query()"
          (ngModelChange)="query.set($event); page.set(1)"
        />
      </div>
      <div class="table-filters">
        <select
          aria-label="Filter by status"
          [ngModel]="status()"
          (ngModelChange)="status.set($event); page.set(1)"
        >
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option></select
        ><span class="record-count">{{ filtered().length }} records</span>
      </div>
    </div>
    <div class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>{{ master().singular }} name</th>
            <th>Code</th>
            @for (column of master().columns; track column.key) {
              <th>{{ column.label }}</th>
            }
            <th>Status</th>
            <th class="actions-heading">Actions</th>
          </tr>
        </thead>
        <tbody>
          @for (row of paged(); track row['id']) {
            <tr>
              <td>
                <div class="record-name">
                  <span class="record-avatar">{{ initials(row[master().name]) }}</span
                  ><span>{{ row[master().name] }}</span>
                </div>
              </td>
              <td>
                <span class="code">{{ row[master().code] }}</span>
              </td>
              @for (column of master().columns; track column.key) {
                <td class="detail-cell" [title]="display()(row, column.key)">
                  {{ display()(row, column.key) }}
                </td>
              }
              <td>
                <span class="status-pill" [class.inactive]="!row['isActive']"
                  ><i></i>{{ row['isActive'] ? 'Active' : 'Inactive' }}</span
                >
              </td>
              <td>
                <div class="row-actions">
                  <button
                    class="text-button"
                    (click)="edit.emit(row)"
                    [disabled]="busy()"
                    [attr.aria-label]="'Edit ' + row[master().name]"
                  >
                    <app-icon name="edit" />Edit</button
                  ><button class="text-button muted" (click)="toggle.emit(row)" [disabled]="busy()">
                    {{ row['isActive'] ? 'Deactivate' : 'Activate' }}</button
                  ><button
                    class="icon-button delete-button"
                    [disabled]="busy()"
                    (click)="remove.emit(row)"
                    [attr.aria-label]="'Delete ' + row[master().name]"
                  >
                    <app-icon name="trash" />
                  </button>
                </div>
              </td>
            </tr>
          } @empty {
            <tr>
              <td [attr.colspan]="master().columns.length + 4">
                <div class="empty-state">
                  <app-icon name="search" />
                  <h3>
                    {{
                      rows().length
                        ? 'No matching records'
                        : 'No ' + master().title.toLowerCase() + ' yet'
                    }}
                  </h3>
                  <p>
                    {{
                      rows().length
                        ? 'Try another search or change the status filter.'
                        : 'Add your first record to get started.'
                    }}
                  </p>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
    <div class="pagination">
      <span
        >Showing {{ filtered().length ? (safePage() - 1) * 10 + 1 : 0 }}–{{
          Math.min(safePage() * 10, filtered().length)
        }}
        of {{ filtered().length }}</span
      >
      <div>
        <button
          class="secondary small"
          [disabled]="safePage() === 1"
          (click)="page.set(safePage() - 1)"
        >
          Previous</button
        ><span class="page-number">{{ safePage() }}</span
        ><button
          class="secondary small"
          [disabled]="safePage() >= pages()"
          (click)="page.set(safePage() + 1)"
        >
          Next
        </button>
      </div>
    </div>
  `,
})
export class DataTable {
  master = input.required<Master>();
  rows = input.required<Row[]>();
  busy = input(false);
  display = input.required<(row: Row, key: string) => string>();
  edit = output<Row>();
  toggle = output<Row>();
  remove = output<Row>();
  constructor() {
    effect(() => {
      this.master();
      this.query.set('');
      this.status.set('all');
      this.page.set(1);
    });
  }
  query = signal('');
  status = signal('all');
  page = signal(1);
  Math = Math;
  filtered = computed(() =>
    this.rows().filter(
      (row) =>
        (this.status() === 'all' || row['isActive'] === (this.status() === 'active')) &&
        [this.master().name, this.master().code, ...this.master().columns.map((c) => c.key)].some(
          (key) =>
            this.display()(row, key).toLowerCase().includes(this.query().trim().toLowerCase()),
        ),
    ),
  );
  pages = computed(() => Math.max(1, Math.ceil(this.filtered().length / 10)));
  safePage = computed(() => Math.min(this.page(), this.pages()));
  paged = computed(() => this.filtered().slice((this.safePage() - 1) * 10, this.safePage() * 10));
  initials(value: string) {
    return (
      value
        ?.split(' ')
        .slice(0, 2)
        .map((x) => x[0])
        .join('')
        .toUpperCase() || '—'
    );
  }
}
