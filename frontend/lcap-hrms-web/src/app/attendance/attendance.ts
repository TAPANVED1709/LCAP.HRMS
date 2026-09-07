import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { SessionService, Envelope, errorMessage } from '../core/api.service';
import { tokenIdentity } from '../employees/employee-fields';
import { AttendanceRow, Today, attendanceState, captureAndSubmit } from './attendance-model';
@Component({
  selector: 'app-attendance',
  imports: [FormsModule],
  templateUrl: './attendance.html',
  styleUrl: './attendance.scss',
})
export class Attendance {
  private http = inject(HttpClient);
  session = inject(SessionService);
  identity = computed(() => tokenIdentity(this.session.token()));
  admin = computed(() => this.identity().roles.some((r) => r === 'HRAdmin' || r === 'SuperAdmin'));
  today = signal<Today | null>(null);
  rows = signal<AttendanceRow[]>([]);
  loading = signal(false);
  phase = signal('');
  error = signal('');
  success = signal('');
  state = attendanceState;
  date = '';
  branchId = '';
  employeeId = '';
  skip = 0;
  readonly take = 50;
  employees = signal<{ id: string; employeeCode: string; fullName: string }[]>([]);
  branches = signal<{ id: string; branchName: string }[]>([]);
  private generation = 0;
  constructor() {
    effect(() => {
      this.session.token();
      this.success.set('');
      this.branches.set([]);
      this.employees.set([]);
      this.skip = 0;
      this.date = '';
      this.branchId = '';
      this.employeeId = '';
      void this.reload();
    });
  }
  async reload() {
    const generation = ++this.generation;
    this.loading.set(true);
    this.error.set('');
    this.today.set(null);
    this.rows.set([]);
    try {
      if (this.identity().employeeId) {
        const r = await firstValueFrom(this.http.get<Envelope<Today>>('/api/attendance/me/today'));
        if (generation === this.generation) this.today.set(r.data);
      }
      if (this.admin()) {
        const branches = await firstValueFrom(
          this.http.get<Envelope<{ id: string; branchName: string }[]>>(
            this.identity().roles.includes('SuperAdmin')
              ? '/api/branches'
              : '/api/companies/' + this.identity().companyId + '/branches',
            {
              params: { take: 1000 },
            },
          ),
        );
        if (generation === this.generation) this.branches.set(branches.data);
        const employees = await firstValueFrom(
          this.http.get<Envelope<{ id: string; employeeCode: string; fullName: string }[]>>(
            '/api/employees/lookup',
            { params: { take: 1000 } },
          ),
        );
        if (generation === this.generation) this.employees.set(employees.data);
      }
      const params: Record<string, string | number> = { skip: this.skip, take: this.take };
      if (this.date) params['date'] = this.date;
      if (this.admin() && this.branchId) params['branchId'] = this.branchId;
      if (this.admin() && this.employeeId.trim()) params['employeeId'] = this.employeeId.trim();
      const list = await firstValueFrom(
        this.http.get<Envelope<AttendanceRow[]>>(
          this.admin() ? '/api/attendance' : '/api/attendance/me',
          { params },
        ),
      );
      if (generation === this.generation) this.rows.set(list.data);
    } catch (e) {
      if (generation === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (generation === this.generation) this.loading.set(false);
    }
  }
  async mark(action: 'check-in' | 'check-out') {
    if (this.phase() || this.loading()) return;
    const token = this.session.token();
    this.error.set('');
    this.success.set('');
    this.phase.set('Locating…');
    try {
      await captureAndSubmit(
        navigator.geolocation,
        async (position) => {
          if (token !== this.session.token())
            throw new Error('Your session changed. Please try again.');
          return await firstValueFrom(
            this.http.post<Envelope<AttendanceRow>>('/api/attendance/' + action, position),
          );
        },
        (value) => {
          if (value) this.phase.set(value);
        },
      );
      if (token !== this.session.token()) return;
      this.success.set(action === 'check-in' ? 'Check-in recorded.' : 'Check-out recorded.');
      await this.reload();
    } catch (e) {
      if (token === this.session.token()) this.error.set(errorMessage(e));
    } finally {
      this.phase.set('');
    }
  }
  filter() {
    this.skip = 0;
    void this.reload();
  }
  page(delta: number) {
    this.skip = Math.max(0, this.skip + delta * this.take);
    void this.reload();
  }
  time(value: string | null, zone: string) {
    return value
      ? new Intl.DateTimeFormat('en-IN', {
          hour: '2-digit',
          minute: '2-digit',
          timeZone: zone,
        }).format(new Date(value))
      : '—';
  }
}
