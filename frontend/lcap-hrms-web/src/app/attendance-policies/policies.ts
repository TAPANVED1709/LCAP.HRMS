import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { RouterLink } from '@angular/router';
import {
  FormControl,
  FormRecord,
  ReactiveFormsModule,
  FormsModule,
  Validators,
} from '@angular/forms';
import { SessionService, Envelope, errorMessage } from '../core/api.service';
import { tokenIdentity } from '../employees/employee-fields';
import { FormControlComponent } from '../shared/form-control';
import { Row } from '../organisation/master-config';
import {
  policyFields,
  policyOptions,
  policyPayload,
  policyError,
  canManagePolicies,
  triggerNames,
  penaltyNames,
  resetNames,
} from './policy-model';
@Component({
  selector: 'app-attendance-policies',
  imports: [RouterLink, ReactiveFormsModule, FormsModule, FormControlComponent],
  templateUrl: './policies.html',
  styleUrl: './policies.scss',
})
export class Policies {
  private http = inject(HttpClient);
  session = inject(SessionService);
  allowed = computed(() => canManagePolicies(tokenIdentity(this.session.token()).roles));
  rows = signal<Row[]>([]);
  companies = signal<{ value: string; label: string }[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  success = signal('');
  search = signal('');
  visible = computed(() =>
    this.rows().filter((r) =>
      (r['policyCode'] + ' ' + r['policyName']).toLowerCase().includes(this.search().toLowerCase()),
    ),
  );
  editing = signal(false);
  readOnly = false;
  id: string | undefined;
  form = new FormRecord<FormControl<any>>({});
  fields = policyFields;
  sections = [...new Set(policyFields.map((f) => f.section))];
  options = policyOptions;
  triggerNames = triggerNames;
  penaltyNames = penaltyNames;
  resetNames = resetNames;
  private generation = 0;
  constructor() {
    effect(() => {
      this.session.token();
      this.rows.set([]);
      this.companies.set([]);
      this.editing.set(false);
      this.error.set('');
      this.success.set('');
      if (this.allowed()) void this.load();
    });
  }
  async load() {
    const g = ++this.generation;
    this.loading.set(true);
    this.error.set('');
    try {
      const r = await firstValueFrom(
        this.http.get<Envelope<Row[]>>('/api/attendance-policies', { params: { take: 1000 } }),
      );
      const identity = tokenIdentity(this.session.token());
      const c = await firstValueFrom(
        this.http.get<Envelope<Row[]>>('/api/companies', { params: { take: 1000 } }),
      );
      if (g === this.generation) {
        this.rows.set(r.data);
        this.companies.set(
          c.data
            .filter((c) => identity.roles.includes('SuperAdmin') || c['id'] === identity.companyId)
            .map((c) => ({ value: c['id'], label: c['companyName'] })),
        );
      }
    } catch (e) {
      if (g === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (g === this.generation) this.loading.set(false);
    }
  }
  open(row?: Row, view = false) {
    this.id = row?.['id'];
    this.readOnly = view;
    this.error.set('');
    this.form = new FormRecord<FormControl<any>>({});
    for (const f of this.fields) {
      const validators = [];
      if (f.required) validators.push(Validators.required);
      if (f.maxLength) validators.push(Validators.maxLength(f.maxLength));
      if (f.min != null) validators.push(Validators.min(f.min));
      if (f.max != null) validators.push(Validators.max(f.max));
      this.form.addControl(
        f.key,
        new FormControl(
          row?.[f.key] ?? f.value ?? (f.type === 'checkbox' ? false : ''),
          validators,
        ),
      );
    }
    if (!row)
      this.form.controls['companyId'].setValue(
        tokenIdentity(this.session.token()).companyId ?? this.companies()[0]?.value ?? '',
      );
    if (row) this.form.controls['companyId'].disable();
    if (view) this.form.disable();
    this.editing.set(true);
  }
  async save() {
    this.form.markAllAsTouched();
    const p = policyPayload(this.form.getRawValue());
    const invalid = policyError(p);
    if (this.form.invalid || invalid) {
      this.error.set(invalid || 'Please correct the highlighted fields.');
      return;
    }
    await this.persist(p, this.id);
  }
  async persist(p: Row, id?: string) {
    if (this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    try {
      await firstValueFrom(
        id
          ? this.http.put('/api/attendance-policies/' + id, p)
          : this.http.post('/api/attendance-policies', p),
      );
      this.editing.set(false);
      this.success.set(
        id ? 'Policy updated. Historical evaluations are unchanged.' : 'Policy created.',
      );
      await this.load();
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  toggle(row: Row) {
    void this.persist({ ...policyPayload(row), isActive: !row['isActive'] }, row['id']);
  }
}
