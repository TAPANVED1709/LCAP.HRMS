import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Envelope, SessionService, errorMessage } from '../core/api.service';
import { tokenIdentity } from '../employees/employee-fields';
import { canManagePolicies, penaltyNames } from './policy-model';
import { Row } from '../organisation/master-config';
@Component({
  selector: 'app-penalties',
  imports: [FormsModule, RouterLink],
  styleUrl: './policies.scss',
  templateUrl: './penalties.html',
})
export class Penalties {
  private http = inject(HttpClient);
  session = inject(SessionService);
  allowed = computed(() => canManagePolicies(tokenIdentity(this.session.token()).roles));
  rows = signal<Row[]>([]);
  loading = signal(false);
  error = signal('');
  fromDate = '';
  toDate = '';
  skip = 0;
  names = penaltyNames;
  private generation = 0;
  constructor() {
    effect(() => {
      this.session.token();
      this.rows.set([]);
      this.error.set('');
      this.skip = 0;
      if (this.allowed()) void this.load();
    });
  }
  async load() {
    const g = ++this.generation;
    this.error.set('');
    this.loading.set(true);
    try {
      const params: Record<string, string | number> = { skip: this.skip, take: 50 };
      if (this.fromDate) params['fromDate'] = this.fromDate;
      if (this.toDate) params['toDate'] = this.toDate;
      const r = await firstValueFrom(
        this.http.get<Envelope<Row[]>>('/api/attendance-penalties', { params }),
      );
      if (g === this.generation) this.rows.set(r.data);
    } catch (e) {
      if (g === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (g === this.generation) this.loading.set(false);
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
}
