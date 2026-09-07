import { Component, inject, signal, effect, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  FormsModule,
  Validators,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService, Envelope, SessionService, errorMessage } from '../core/api.service';
import { FormControlComponent } from '../shared/form-control';
import { Field, Row } from '../organisation/master-config';
import {
  employeeFields,
  statuses,
  employmentTypes,
  maskSensitive,
  sensitiveKeys,
  employeePayload,
  dependentOptions,
  employeeValidation,
  tokenIdentity,
} from './employee-fields';

@Component({
  selector: 'app-employees',
  imports: [RouterLink, ReactiveFormsModule, FormsModule, FormControlComponent],
  templateUrl: './employees.html',
  styleUrl: './employees.scss',
})
export class Employees {
  private api = inject(ApiService);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  session = inject(SessionService);
  fields = employeeFields;
  sections = ['Personal', 'Employment', 'Organisation', 'Statutory', 'Bank'];
  statuses = statuses;
  mode = signal('list');
  id = signal<string | undefined>(undefined);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  loadFailed = signal(false);
  notice = signal('');
  rows = signal<Row[]>([]);
  detail = signal<Row | undefined>(undefined);
  chain = signal<Row[]>([]);
  lookups = signal<Partial<Record<string, Row[]>>>({});
  reveal = signal(false);
  search = '';
  status = '';
  department = '';
  branch = '';
  page = signal(0);
  hasNext = signal(false);
  private version = 0;
  identity = computed(() => tokenIdentity(this.session.token()));
  canWrite = computed(() =>
    this.identity().roles.some((r) => ['SuperAdmin', 'HRAdmin'].includes(r)),
  );
  form = new FormGroup<Record<string, FormControl>>({});
  constructor() {
    this.notice.set(this.router.getCurrentNavigation()?.extras.state?.['notice'] ?? '');
    for (const f of this.fields)
      this.form.addControl(
        f.key,
        new FormControl(f.key === 'isActive' ? true : null, [
          ...(f.required ? [Validators.required] : []),
          ...(f.maxLength ? [Validators.maxLength(f.maxLength)] : []),
          ...(f.type === 'email' ? [Validators.email] : []),
        ]),
      );
    this.form.controls['companyId'].valueChanges.subscribe(() => {
      for (const key of [
        'branchId',
        'departmentId',
        'designationId',
        'shiftId',
        'workLocationId',
        'reportingManagerId',
      ])
        this.form.controls[key].setValue(null, { emitEvent: false });
    });
    this.form.controls['branchId'].valueChanges.subscribe(() =>
      this.form.controls['workLocationId'].setValue(null, { emitEvent: false }),
    );
    this.route.paramMap.subscribe((params) => {
      this.id.set(params.get('id') ?? undefined);
      const path = this.route.snapshot.routeConfig?.path;
      this.mode.set(
        path?.endsWith('new')
          ? 'new'
          : path?.endsWith('edit')
            ? 'edit'
            : path?.endsWith('my-team')
              ? 'team'
              : params.has('id')
                ? 'view'
                : 'list',
      );
      this.reveal.set(false);
      void this.load();
    });
    effect(() => {
      this.session.token();
      void this.load();
    });
  }
  control(f: Field) {
    return this.form.controls[f.key];
  }
  sectionFields(section: string) {
    return this.fields.filter((f) => f.section === section);
  }
  options(f: Field) {
    if (f.key === 'employeeStatus') return statuses;
    if (f.key === 'employmentType') return employmentTypes;
    const names: Record<string, string> = {
      companyId: 'companyName',
      branchId: 'branchName',
      departmentId: 'departmentName',
      designationId: 'designationName',
      shiftId: 'shiftName',
      workLocationId: 'locationName',
      reportingManagerId: 'fullName',
    };
    return dependentOptions(
      f.key,
      this.lookups()[f.key] ?? [],
      this.form.getRawValue(),
      this.id(),
    ).map((r) => ({
      value: r['id'],
      label:
        r[names[f.key]] + (f.key === 'reportingManagerId' ? ' (' + r['employeeCode'] + ')' : ''),
    }));
  }
  async load() {
    const version = ++this.version;
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.detail.set(undefined);
    this.rows.set([]);
    this.lookups.set({});
    try {
      if (['new', 'edit'].includes(this.mode()) && !this.canWrite())
        throw new Error('HR administrator access is required to edit employees.');
      if (['list', 'team'].includes(this.mode())) {
        const resource = this.mode() === 'team' ? 'employees/my-team' : 'employees';
        const params: Record<string, string | number> = { skip: this.page() * 25, take: 26 };
        if (this.mode() === 'list') {
          if (this.search) params['search'] = this.search;
          if (this.status) params['status'] = this.status;
          if (this.department) params['departmentId'] = this.department;
          if (this.branch) params['branchId'] = this.branch;
        }
        const response = await firstValueFrom(
          this.http.get<Envelope<Row[]>>('/api/' + resource, { params }),
        );
        if (version !== this.version) return;
        this.hasNext.set(response.data.length > 25);
        this.rows.set(response.data.slice(0, 25));
      }
      if (['new', 'edit', 'list'].includes(this.mode())) {
        const resources: Record<string, string> =
          this.mode() === 'list'
            ? { branchId: 'branches', departmentId: 'departments' }
            : {
                companyId: 'companies',
                branchId: 'branches',
                departmentId: 'departments',
                designationId: 'designations',
                shiftId: 'shifts',
                workLocationId: 'work-locations',
                reportingManagerId: 'employees/lookup',
              };
        const entries = await Promise.all(
          Object.entries(resources).map(
            async ([key, resource]) => [key, await this.api.all(resource)] as const,
          ),
        );
        if (version !== this.version) return;
        this.lookups.set(Object.fromEntries(entries));
      }
      if (['view', 'edit'].includes(this.mode())) {
        const response = await this.api.get('employees', this.id()!);
        if (version !== this.version) return;
        this.detail.set(response.data);
        if (this.mode() === 'edit') {
          this.form.reset(employeePayload(response.data), { emitEvent: false });
          this.form.controls['companyId'].disable({ emitEvent: false });
        } else {
          const chain = await this.api.all('employees/' + this.id() + '/reporting-chain');
          if (version !== this.version) return;
          this.chain.set(chain);
        }
      } else if (this.mode() === 'new') {
        this.form.enable({ emitEvent: false });
        this.form.reset(
          {
            isActive: true,
            employeeStatus: '1',
            employmentType: '5',
            companyId: this.identity().companyId ?? null,
          },
          { emitEvent: false },
        );
      }
    } catch (e) {
      if (version === this.version) {
        this.loadFailed.set(true);
        this.error.set(errorMessage(e));
      }
    } finally {
      if (version === this.version) this.loading.set(false);
    }
  }
  async save() {
    this.form.markAllAsTouched();
    const body = employeePayload(this.form.getRawValue());
    const message = employeeValidation(body);
    if (this.form.invalid || message) {
      this.error.set(message || 'Check the highlighted fields.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    try {
      const response = await this.api.save('employees', body, this.id());
      this.notice.set('Employee saved successfully.');
      await this.router.navigate(['/employees', response.data['id']], {
        state: { notice: 'Employee saved successfully.' },
      });
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  async toggle() {
    const record = this.detail();
    if (!record) return;
    this.saving.set(true);
    this.error.set('');
    try {
      const body = employeePayload(record);
      body['isActive'] = !record['isActive'];
      body['employeeStatus'] = body['isActive'] ? 1 : 2;
      const response = await this.api.save('employees', body, record['id']);
      this.detail.set(response.data);
      this.notice.set('Employee status updated.');
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  async remove() {
    if (!this.id() || !confirm('Delete this employee? The audit record will be retained.')) return;
    this.saving.set(true);
    try {
      await this.api.remove('employees', this.id()!);
      await this.router.navigate(['/employees'], { state: { notice: 'Employee deleted.' } });
      this.notice.set('Employee deleted.');
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  filter() {
    this.page.set(0);
    void this.load();
  }
  scrollTo(section: string) {
    document
      .getElementById('section-' + section)
      ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
  next(delta: number) {
    this.page.update((p) => Math.max(0, p + delta));
    void this.load();
  }
  statusName(value: unknown) {
    return statuses.find((s) => s.value === String(value))?.label ?? '—';
  }
  display(f: Field) {
    const record = this.detail();
    let value = record?.[f.key];
    if (f.key.endsWith('Id'))
      value =
        record?.[f.key === 'reportingManagerId' ? 'reportingManagerName' : f.key.slice(0, -2)];
    if (f.key === 'employeeStatus') return this.statusName(value);
    if (f.key === 'employmentType')
      return employmentTypes.find((t) => t.value === String(value))?.label ?? '—';
    if (sensitiveKeys.includes(f.key) && !this.reveal()) return maskSensitive(value);
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    return value ?? '—';
  }
}
