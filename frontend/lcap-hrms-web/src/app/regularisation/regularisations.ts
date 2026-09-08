import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Envelope, SessionService, errorMessage } from '../core/api.service';
import { tokenIdentity } from '../employees/employee-fields';
import {
  requestTypes,
  statuses,
  requestFields,
  requestError,
  localToUtc,
  canReview,
  canReadRegister,
  canCancel,
  RequestRow,
  EffectiveState,
} from './regularisation-model';
@Component({
  selector: 'app-regularisations',
  imports: [FormsModule, RouterLink],
  templateUrl: './regularisations.html',
  styleUrl: './regularisations.scss',
})
export class Regularisations {
  private http = inject(HttpClient);
  private router = inject(Router);
  session = inject(SessionService);
  identity = computed(() => tokenIdentity(this.session.token()));
  manager = computed(() => canReview(this.identity().roles));
  hr = computed(() => canReadRegister(this.identity().roles));
  mode = this.router.url.includes('/new')
    ? 'new'
    : this.router.url.includes('/manager/')
      ? 'team'
      : this.router.url.endsWith('/admin')
        ? 'admin'
        : 'me';
  allowed = computed(() =>
    this.mode === 'team'
      ? this.manager()
      : this.mode === 'admin'
        ? this.hr()
        : !!this.identity().employeeId,
  );
  types = requestTypes;
  statuses = statuses;
  fields = requestFields;
  canCancel = canCancel;
  rows = signal<RequestRow[]>([]);
  loading = signal(false);
  busy = signal(false);
  error = signal('');
  success = signal('');
  preview = signal<EffectiveState | null>(null);
  selected = signal<RequestRow | null>(null);
  audit = signal<{ action: string; occurredAt: string; reason: string | null }[]>([]);
  date = '';
  type = 1;
  checkIn = '';
  checkOut = '';
  reason = '';
  note = '';
  remarks = '';
  action = '';
  status = '';
  employeeId = '';
  managerId = '';
  branchId = '';
  fromDate = '';
  toDate = '';
  skip = 0;
  employees = signal<{ id: string; employeeCode: string; fullName: string }[]>([]);
  branches = signal<{ id: string; branchName: string }[]>([]);
  private generation = 0;
  constructor() {
    effect(() => {
      this.session.token();
      ++this.generation;
      this.rows.set([]);
      this.preview.set(null);
      this.selected.set(null);
      this.audit.set([]);
      this.success.set('');
      this.error.set('');
      this.skip = 0;
      this.employeeId = '';
      this.managerId = '';
      this.branchId = '';
      this.employees.set([]);
      this.branches.set([]);
      if (this.allowed() && this.mode !== 'new') void this.load();
    });
  }
  private async get<T>(path: string, params: Record<string, string | number> = {}) {
    return (await firstValueFrom(this.http.get<Envelope<T>>(path, { params }))).data;
  }
  async load() {
    const g = ++this.generation;
    this.loading.set(true);
    this.error.set('');
    try {
      const params: Record<string, string | number> = { skip: this.skip, take: 50 };
      for (const key of [
        'status',
        'employeeId',
        'managerId',
        'branchId',
        'fromDate',
        'toDate',
      ] as const)
        if (this[key]) params[key] = this[key];
      const suffix = this.mode === 'team' ? '/my-team' : this.mode === 'me' ? '/me' : '';
      const list = await this.get<RequestRow[]>('/api/attendance-regularisations' + suffix, params);
      if (g !== this.generation) return;
      this.rows.set(list);
      if (this.mode === 'team')
        this.employees.set([
          ...new Map(
            list.map((r) => [
              r.employeeId,
              { id: r.employeeId, employeeCode: r.employeeCode, fullName: r.employeeName },
            ]),
          ).values(),
        ]);
      if (this.mode === 'admin') {
        const [employees, branches] = await Promise.all([
          this.get<{ id: string; employeeCode: string; fullName: string }[]>(
            '/api/employees/lookup',
            { take: 1000 },
          ),
          this.get<{ id: string; branchName: string }[]>(
            this.identity().roles.includes('SuperAdmin')
              ? '/api/branches'
              : '/api/companies/' + this.identity().companyId + '/branches',
            { take: 1000 },
          ),
        ]);
        if (g === this.generation) {
          this.employees.set(employees);
          this.branches.set(branches);
        }
      }
    } catch (e) {
      if (g === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (g === this.generation) this.loading.set(false);
    }
  }
  async original() {
    const g = ++this.generation;
    this.preview.set(null);
    this.error.set('');
    if (!this.date) return;
    this.loading.set(true);
    try {
      const state = await this.get<EffectiveState>('/api/attendance/effective/me', {
        date: this.date,
      });
      if (g === this.generation) this.preview.set(state);
    } catch (e) {
      if (g === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (g === this.generation) this.loading.set(false);
    }
  }
  changeType() {
    if (!this.fields(+this.type).checkIn) this.checkIn = '';
    if (!this.fields(+this.type).checkOut) this.checkOut = '';
  }
  async submit() {
    if (this.busy() || this.loading()) return;
    const message = requestError(+this.type, this.date, this.reason, this.checkIn, this.checkOut);
    if (message) {
      this.error.set(message);
      return;
    }
    if (this.preview()?.attendanceDate !== this.date) {
      this.error.set('Load original attendance for the selected date first.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const token = this.session.token();
    try {
      const zone = this.preview()!.timeZoneId;
      const body = {
        attendanceDate: this.date,
        requestType: +this.type,
        requestedCheckInTime: localToUtc(this.checkIn, zone),
        requestedCheckOutTime: localToUtc(this.checkOut, zone),
        employeeReason: this.reason.trim(),
        supportingNote: this.note.trim() || null,
      };
      const row = (
        await firstValueFrom(
          this.http.post<Envelope<RequestRow>>('/api/attendance-regularisations', body),
        )
      ).data;
      if (token !== this.session.token()) return;
      this.selected.set(row);
      this.success.set('Request submitted. Pending Manager Approval.');
      this.reason = '';
      this.note = '';
      this.checkIn = '';
      this.checkOut = '';
    } catch (e) {
      if (token === this.session.token()) this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  async view(row: RequestRow) {
    const g = this.generation;
    this.selected.set(row);
    this.action = '';
    this.audit.set([]);
    try {
      const entries = await this.get<
        { action: string; occurredAt: string; reason: string | null }[]
      >('/api/attendance-regularisations/' + row.id + '/audit');
      if (g === this.generation && this.selected()?.id === row.id) this.audit.set(entries);
    } catch (e) {
      if (g === this.generation) this.error.set(errorMessage(e));
    }
  }
  review(row: RequestRow, action: string) {
    this.selected.set(row);
    this.action = action;
    this.success.set('');
    this.remarks = '';
    this.error.set('');
  }
  async confirm() {
    const row = this.selected();
    if (!row || this.busy()) return;
    if (this.action !== 'approve' && !this.remarks.trim()) {
      this.error.set(
        this.action === 'reject'
          ? 'Rejection remarks are required.'
          : 'Cancellation reason is required.',
      );
      return;
    }
    const action = this.action;
    this.busy.set(true);
    this.error.set('');
    const token = this.session.token();
    try {
      const updated = (
        await firstValueFrom(
          this.http.post<Envelope<RequestRow>>(
            '/api/attendance-regularisations/' + row.id + '/' + action,
            action === 'cancel'
              ? { cancellationReason: this.remarks.trim() }
              : { reviewerRemarks: this.remarks.trim() || null },
          ),
        )
      ).data;
      if (token !== this.session.token()) return;
      this.selected.set(null);
      this.action = '';
      this.success.set(
        action === 'approve'
          ? 'Request approved and correction applied.'
          : action === 'reject'
            ? 'Request rejected.'
            : 'Request cancelled.',
      );
      await this.load();
    } catch (e) {
      if (token === this.session.token()) this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  filter() {
    this.skip = 0;
    void this.load();
  }
  page(delta: number) {
    this.skip = Math.max(0, this.skip + delta * 50);
    void this.load();
  }
  format(value: string | null, zone = 'UTC') {
    return value
      ? new Intl.DateTimeFormat('en-IN', {
          dateStyle: 'medium',
          timeStyle: 'short',
          timeZone: zone,
        }).format(new Date(value))
      : '—';
  }
}
