import type { Field, Row } from '../organisation/master-config';
const field = (
  key: string,
  label: string,
  section: string,
  type = 'text',
  required = false,
  maxLength = 100,
): Field => ({ key, label, section, type, required, maxLength });
export const employeeFields: Field[] = [
  field('firstName', 'First name', 'Personal', 'text', true),
  field('middleName', 'Middle name', 'Personal'),
  field('lastName', 'Last name', 'Personal'),
  field('mobileNumber', 'Mobile number', 'Personal', 'tel', true, 25),
  field('alternateMobileNumber', 'Alternate mobile', 'Personal', 'tel', false, 25),
  field('personalEmail', 'Personal email', 'Personal', 'email', false, 254),
  field('officialEmail', 'Official email', 'Personal', 'email', false, 254),
  field('dateOfBirth', 'Date of birth', 'Personal', 'date'),
  field('gender', 'Gender', 'Personal', 'text', false, 50),
  field('bloodGroup', 'Blood group', 'Personal', 'text', false, 10),
  field('profilePhotoUrl', 'Profile photo URL', 'Personal', 'url', false, 2048),
  field('employeeCode', 'Employee code', 'Employment', 'text', true, 50),
  field('dateOfJoining', 'Joining date', 'Employment', 'date', true),
  field('dateOfConfirmation', 'Confirmation date', 'Employment', 'date'),
  field('probationEndDate', 'Probation end date', 'Employment', 'date'),
  field('employmentType', 'Employment type', 'Employment', 'select', true),
  field('employeeStatus', 'Employee status', 'Employment', 'select', true),
  field('dateOfResignation', 'Resignation date', 'Employment', 'date'),
  field('lastWorkingDate', 'Last working date', 'Employment', 'date'),
  field('exitReason', 'Exit reason', 'Employment', 'textarea', false, 1000),
  field('notes', 'Notes', 'Employment', 'textarea', false, 2000),
  field('isActive', 'Active record', 'Employment', 'checkbox'),
  field('companyId', 'Company', 'Organisation', 'select', true),
  field('branchId', 'Branch', 'Organisation', 'select', true),
  field('departmentId', 'Department', 'Organisation', 'select', true),
  field('designationId', 'Designation', 'Organisation', 'select', true),
  field('shiftId', 'Shift', 'Organisation', 'select', true),
  field('workLocationId', 'Work location', 'Organisation', 'select', true),
  field('reportingManagerId', 'Reporting manager', 'Organisation', 'select'),
  field('pan', 'PAN', 'Statutory', 'text', false, 10),
  field('aadhaarNumber', 'Aadhaar number', 'Statutory', 'text', false, 12),
  field('uan', 'UAN', 'Statutory', 'text', false, 20),
  field('esicNumber', 'ESIC number', 'Statutory', 'text', false, 20),
  field('bankName', 'Bank name', 'Bank', 'text', false, 200),
  field('accountHolderName', 'Account holder name', 'Bank', 'text', false, 200),
  field('bankAccountNumber', 'Bank account number', 'Bank', 'text', false, 34),
  field('ifscCode', 'IFSC code', 'Bank', 'text', false, 11),
];
export const statuses = [
  { value: '1', label: 'Active' },
  { value: '2', label: 'Inactive' },
  { value: '3', label: 'On leave' },
  { value: '4', label: 'Terminated' },
  { value: '5', label: 'On notice' },
  { value: '6', label: 'Exited' },
  { value: '7', label: 'Suspended' },
];
export const employmentTypes = [
  { value: '5', label: 'Permanent' },
  { value: '6', label: 'Probation' },
  { value: '3', label: 'Contract' },
  { value: '4', label: 'Intern' },
  { value: '7', label: 'Consultant' },
  { value: '8', label: 'Temporary' },
  { value: '1', label: 'Full time' },
  { value: '2', label: 'Part time' },
];
export function maskSensitive(value: unknown): string {
  const text = String(value ?? '');
  return text ? '*'.repeat(Math.max(0, text.length - 4)) + text.slice(-4) : '—';
}
export const sensitiveKeys = ['pan', 'aadhaarNumber', 'uan', 'esicNumber', 'bankAccountNumber'];
export function employeePayload(raw: Row): Row {
  const result: Row = {};
  for (const f of employeeFields) {
    let value = raw[f.key];
    if (typeof value === 'string') value = value.trim();
    if (value === '') value = null;
    if (['employeeStatus', 'employmentType'].includes(f.key) && value != null)
      value = Number(value);
    result[f.key] = value;
  }
  return result;
}
export function dependentOptions(key: string, rows: Row[], values: Row, currentId?: string): Row[] {
  return rows.filter(
    (r) =>
      (key === 'companyId' || r['companyId'] === values['companyId']) &&
      (key !== 'workLocationId' || r['branchId'] === values['branchId']) &&
      (key !== 'reportingManagerId' || r['id'] !== currentId),
  );
}
export function employeeValidation(v: Row, today = new Date().toISOString().slice(0, 10)): string {
  for (const f of employeeFields) {
    const value = v[f.key];
    if (f.required && (value == null || String(value).trim() === ''))
      return f.label + ' is required.';
    if (typeof value === 'string' && value.length > (f.maxLength ?? Infinity))
      return f.label + ' is too long.';
  }
  const patterns: Record<string, RegExp> = {
    mobileNumber: /^\+?[0-9][0-9 ()-]{6,23}[0-9]$/,
    alternateMobileNumber: /^\+?[0-9][0-9 ()-]{6,23}[0-9]$/,
    pan: /^[A-Z]{5}[0-9]{4}[A-Z]$/,
    aadhaarNumber: /^[0-9]{12}$/,
    ifscCode: /^[A-Z]{4}0[A-Z0-9]{6}$/,
    uan: /^[0-9]{12}$/,
    esicNumber: /^[0-9]{10,17}$/,
    bankAccountNumber: /^[A-Za-z0-9]{6,34}$/,
  };
  for (const [key, pattern] of Object.entries(patterns))
    if (v[key] && !pattern.test(v[key]))
      return 'Invalid ' + (employeeFields.find((f) => f.key === key)?.label ?? key) + ' format.';
  for (const key of ['personalEmail', 'officialEmail'])
    if (v[key] && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v[key])) return 'Enter a valid email address.';
  if (v['dateOfBirth'] && v['dateOfBirth'] > today) return 'Date of birth cannot be in the future.';
  if (v['dateOfBirth'] && v['dateOfBirth'] >= v['dateOfJoining'])
    return 'Date of birth must precede joining date.';
  const latest = new Date(today);
  latest.setUTCFullYear(latest.getUTCFullYear() + 1);
  if (v['dateOfJoining'] < '1900-01-01' || v['dateOfJoining'] > latest.toISOString().slice(0, 10))
    return 'Joining date is outside the supported range.';
  for (const key of [
    'dateOfConfirmation',
    'lastWorkingDate',
    'probationEndDate',
    'dateOfResignation',
  ])
    if (v[key] && v[key] < v['dateOfJoining'])
      return 'Employment dates cannot precede joining date.';
  if (
    v['lastWorkingDate'] &&
    v['dateOfResignation'] &&
    v['lastWorkingDate'] < v['dateOfResignation']
  )
    return 'Last working date cannot precede resignation.';
  return '';
}
// UI hints only. The API independently validates the signature, roles and scope.
export function tokenIdentity(token: string): {
  roles: string[];
  employeeId?: string;
  companyId?: string;
} {
  try {
    const p = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return {
      roles: Array.isArray(p.role) ? p.role : [p.role].filter(Boolean),
      employeeId: p.employee_id,
      companyId: p.company_id,
    };
  } catch {
    return { roles: [] };
  }
}
