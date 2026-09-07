import { requestBody, crossFieldError } from './master-utils';
import { Component, ElementRef, ViewChild, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService, SessionService, errorMessage } from '../core/api.service';
import { MASTERS, Master, Row, Field } from './master-config';
import { Icon } from '../shared/icon';
import { DataTable } from '../shared/data-table';
import { FormControlComponent } from '../shared/form-control';
@Component({
  selector: 'app-organisation',
  imports: [
    RouterLink,
    RouterLinkActive,
    ReactiveFormsModule,
    Icon,
    DataTable,
    FormControlComponent,
  ],
  templateUrl: './organisation.html',
})
export class Organisation {
  api = inject(ApiService);
  session = inject(SessionService);
  route = inject(ActivatedRoute);
  masters = MASTERS;
  master = signal<Master>(MASTERS[0]);
  records = signal<Record<string, Row[]>>({});
  loading = signal(true);
  error = signal('');
  notice = signal('');
  saving = signal(false);
  formError = signal('');
  actionError = signal('');
  editing = signal<Row | null>(null);
  deleting = signal<Row | null>(null);
  form = new FormGroup<Record<string, FormControl>>({});
  @ViewChild('editor') editor!: ElementRef<HTMLDialogElement>;
  @ViewChild('confirmation') confirmation!: ElementRef<HTMLDialogElement>;
  private generation = 0;
  rows = computed(() => this.records()[this.master().key] || []);
  active = computed(() => this.rows().filter((x) => x['isActive']).length);
  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.master.set(MASTERS.find((m) => m.key === params.get('master')) || MASTERS[0]);
      this.notice.set('');
      this.actionError.set('');
      this.editor?.nativeElement.close();
      this.confirmation?.nativeElement.close();
    });
    effect(() => {
      this.session.token();
      void this.load();
    });
  }
  async load() {
    const generation = ++this.generation;
    this.loading.set(true);
    this.error.set('');
    try {
      const values = await Promise.all(
        MASTERS.map(async (m) => [m.key, await this.api.all(m.key)] as const),
      );
      if (generation !== this.generation) return;
      this.records.set(Object.fromEntries(values));
      const companies = this.records()['companies'];
      this.session.companyName.set(
        companies.find((x) => x['companyCode'] === 'LCAP')?.['companyName'] ||
          companies[0]?.['companyName'] ||
          'LCAP',
      );
    } catch (e) {
      if (generation === this.generation) this.error.set(errorMessage(e));
    } finally {
      if (generation === this.generation) this.loading.set(false);
    }
  }
  display = (row: Row, key: string): string => {
    const value = row[key];
    if (value === null || value === undefined || value === '') return '—';
    const relation = (
      {
        companyId: ['companies', 'companyName'],
        branchId: ['branches', 'branchName'],
        parentDepartmentId: ['departments', 'departmentName'],
      } as Record<string, string[]>
    )[key];
    if (relation)
      return (
        this.records()[relation[0]]?.find((x) => x['id'] === value)?.[relation[1]] || 'Unavailable'
      );
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (key === 'startTime' || key === 'endTime') {
      const [h, m] = String(value).split(':');
      return (+h % 12 || 12) + ':' + m + ' ' + (+h < 12 ? 'AM' : 'PM');
    }
    return String(value);
  };
  control(field: Field) {
    return this.form.controls[field.key];
  }
  options(field: Field) {
    const companyId = this.form.controls['companyId']?.value;
    let rows: Row[] = [];
    let label = '';
    if (field.key === 'companyId') {
      rows = this.records()['companies'] || [];
      label = 'companyName';
    }
    if (field.key === 'branchId') {
      rows = (this.records()['branches'] || []).filter((x) => x['companyId'] === companyId);
      label = 'branchName';
    }
    if (field.key === 'parentDepartmentId') {
      rows = (this.records()['departments'] || []).filter(
        (x) => x['companyId'] === companyId && !this.isDescendant(x),
      );
      label = 'departmentName';
    }
    return rows.map((x) => ({
      value: x['id'],
      label: x[label] + (x['isActive'] ? '' : ' (inactive)'),
    }));
  }
  private isDescendant(row: Row) {
    const visited = new Set<string>();
    let cursor: Row | undefined = row;
    while (cursor) {
      if (cursor['id'] === this.editing()?.['id'] || visited.has(cursor['id'])) return true;
      visited.add(cursor['id']);
      cursor = (this.records()['departments'] || []).find(
        (x) => x['id'] === cursor?.['parentDepartmentId'],
      );
    }
    return false;
  }
  open(row: Row | null = null) {
    this.editing.set(row);
    this.formError.set('');
    const controls: Record<string, FormControl> = {};
    for (const field of this.master().fields) {
      const validators = [];
      if (field.required) validators.push(Validators.required);
      if (field.required && !field.type) validators.push(Validators.pattern(/\S/));
      if (field.maxLength) validators.push(Validators.maxLength(field.maxLength));
      if (field.min !== undefined) validators.push(Validators.min(field.min));
      if (field.max !== undefined) validators.push(Validators.max(field.max));
      if (field.type === 'email') validators.push(Validators.email);
      if (field.type === 'url') validators.push(Validators.pattern(/^https?:\/\/\S+$/i));
      if (field.type === 'tel') validators.push(Validators.pattern(/^\+?[0-9() .-]+$/));
      if (field.key === 'payrollCurrency') validators.push(Validators.pattern(/^[A-Za-z]{3}$/));
      if (field.type === 'number' && !field.step) validators.push(Validators.pattern(/^-?\d+$/));
      let value = row ? (row[field.key] ?? null) : (field.value ?? null);
      if (!row && field.key === 'companyId' && this.records()['companies']?.length === 1)
        value = this.records()['companies'][0]['id'];
      controls[field.key] = new FormControl(value, validators);
    }
    this.form = new FormGroup(controls);
    if (row && this.master().key === 'departments') controls['companyId'].disable();
    controls['companyId']?.valueChanges.subscribe((id) => {
      controls['branchId']?.setValue(null);
      controls['parentDepartmentId']?.setValue(null);
      if (this.master().key === 'branches' && !row)
        controls['state']?.setValue(
          this.records()['companies']?.find((x) => x['id'] === id)?.['state'] ?? null,
        );
    });
    if (!row && this.master().key === 'branches' && controls['companyId'].value)
      controls['state'].setValue(
        this.records()['companies']?.find((x) => x['id'] === controls['companyId'].value)?.[
          'state'
        ] ?? null,
      );
    this.editor.nativeElement.showModal();
  }
  close() {
    if (!this.saving()) this.editor.nativeElement.close();
  }
  private payload(values: Row, master = this.master()) {
    return requestBody(master, values);
  }
  async save() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      const field = this.master().fields.find((f) => this.form.controls[f.key].invalid);
      if (field) this.editor.nativeElement.querySelector<HTMLElement>('#' + field.key)?.focus();
      return;
    }
    const body = this.payload(this.form.getRawValue());
    this.formError.set('');
    const validation = crossFieldError(this.master().key, body);
    if (validation) {
      this.formError.set(validation);
      return;
    }
    this.saving.set(true);
    try {
      await this.api.save(this.master().key, body, this.editing()?.['id']);
      this.editor.nativeElement.close();
      this.notice.set(
        this.master().singular +
          (this.editing() ? ' updated successfully.' : ' added successfully.'),
      );
      await this.load();
    } catch (e) {
      this.formError.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  async toggle(row: Row) {
    const master = this.master();
    this.saving.set(true);
    this.notice.set('');
    this.actionError.set('');
    try {
      const latest = await this.api.get(master.key, row['id']);
      await this.api.save(
        master.key,
        { ...this.payload(latest.data, master), isActive: !row['isActive'] },
        row['id'],
      );
      this.notice.set(master.singular + (row['isActive'] ? ' deactivated.' : ' activated.'));
      await this.load();
    } catch (e) {
      this.actionError.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
  askDelete(row: Row) {
    this.deleting.set(row);
    this.formError.set('');
    this.confirmation.nativeElement.showModal();
  }
  async remove() {
    this.saving.set(true);
    this.formError.set('');
    try {
      await this.api.remove(this.master().key, this.deleting()!['id']);
      this.confirmation.nativeElement.close();
      this.notice.set(this.master().singular + ' deleted successfully.');
      await this.load();
    } catch (e) {
      this.formError.set(errorMessage(e));
    } finally {
      this.saving.set(false);
    }
  }
}
