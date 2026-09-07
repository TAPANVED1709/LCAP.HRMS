import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';
const source = await readFile(
  new URL('../src/app/employees/employee-fields.ts', import.meta.url),
  'utf8',
);
const { outputText } = ts.transpileModule(source, {
  compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
});
const {
  employeeFields,
  employeePayload,
  employeeValidation,
  dependentOptions,
  maskSensitive,
  tokenIdentity,
} = await import('data:text/javascript;base64,' + Buffer.from(outputText).toString('base64'));
const valid = {
  companyId: 'company',
  branchId: 'branch',
  departmentId: 'department',
  designationId: 'designation',
  shiftId: 'shift',
  workLocationId: 'location',
  employeeCode: 'TEST',
  firstName: 'Test',
  mobileNumber: '0000000000',
  dateOfJoining: '2026-01-01',
  employeeStatus: 1,
  employmentType: 5,
  isActive: true,
};
test('employee form covers writable backend fields only', async () => {
  const backend = await readFile(
    new URL(
      '../../../backend/LCAP.HRMS.Application/Employees/DTOs/EmployeeWriteRequest.cs',
      import.meta.url,
    ),
    'utf8',
  );
  const camel = (s) => s.replace(/^[A-Z]+(?=[A-Z][a-z]|$)|^[A-Z]/, (x) => x.toLowerCase());
  const fields = [...backend.matchAll(/public [\w?]+ (\w+) \{ get; set; \}/g)]
    .map((m) => camel(m[1]))
    .sort();
  assert.deepEqual(employeeFields.map((f) => f.key).sort(), fields);
});
test('form required values and date ordering errors', () => {
  assert.equal(employeeValidation(valid), '');
  assert.match(employeeValidation({ ...valid, firstName: ' ' }), /required/);
  assert.match(employeeValidation({ ...valid, lastWorkingDate: '2025-01-01' }), /precede/);
  assert.match(employeeValidation({ ...valid, dateOfBirth: '2099-01-01' }), /future/);
});
test('sensitive format validation does not echo sensitive value', () => {
  for (const key of ['pan', 'aadhaarNumber', 'ifscCode']) {
    const error = employeeValidation({ ...valid, [key]: 'sensitive-invalid' });
    assert.match(error, /Invalid|too long/);
    assert.ok(!error.includes('sensitive-invalid'));
  }
});
test('organisation options filter company and branch, managers exclude self', () => {
  const rows = [
    { id: 'self', companyId: 'A', branchId: 'X' },
    { id: 'other', companyId: 'A', branchId: 'Y' },
    { id: 'foreign', companyId: 'B', branchId: 'X' },
  ];
  assert.deepEqual(
    dependentOptions('branchId', rows, { companyId: 'A' }).map((r) => r.id),
    ['self', 'other'],
  );
  assert.deepEqual(
    dependentOptions('workLocationId', rows, { companyId: 'A', branchId: 'X' }).map((r) => r.id),
    ['self'],
  );
  assert.deepEqual(
    dependentOptions('reportingManagerId', rows, { companyId: 'A' }, 'self').map((r) => r.id),
    ['other'],
  );
});
test('masking hides all but last four and handles empty identifiers', () => {
  assert.equal(maskSensitive('000000009012'), '********9012');
  assert.equal(maskSensitive(null), '—');
});
test('edit payload excludes response metadata and preserves nulls and false', () => {
  const result = employeePayload({
    ...valid,
    pan: '',
    notes: ' note ',
    isActive: false,
    createdBy: 'do-not-send',
    fullName: 'Derived',
    id: 'id',
  });
  assert.equal(result.pan, null);
  assert.equal(result.notes, 'note');
  assert.equal(result.isActive, false);
  assert.ok(!('createdBy' in result));
  assert.ok(!('id' in result));
});
test('identity hints parse roles and missing/invalid token is unprivileged', () => {
  assert.deepEqual(tokenIdentity('invalid').roles, []);
  const token =
    'x.' +
    Buffer.from(JSON.stringify({ role: ['HRAdmin'], company_id: 'C' })).toString('base64url') +
    '.x';
  assert.deepEqual(tokenIdentity(token).roles, ['HRAdmin']);
});
test('list and error templates contain no sensitive grid bindings', async () => {
  const html = await readFile(
    new URL('../src/app/employees/employees.html', import.meta.url),
    'utf8',
  );
  const grid = html.slice(html.indexOf('<table>'));
  for (const key of ['pan', 'aadhaarNumber', 'bankAccountNumber'])
    assert.ok(!grid.includes("row['" + key + "']"));
  assert.ok(grid.includes("row['fullName']"));
  assert.ok(html.includes('role="alert"'));
  assert.ok(html.includes('Loading employee data'));
});
