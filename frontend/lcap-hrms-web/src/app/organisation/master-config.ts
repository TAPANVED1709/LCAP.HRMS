export type Row = Record<string, any>;
export interface Field {
  key: string;
  label: string;
  type?: string;
  required?: boolean;
  maxLength?: number;
  min?: number;
  max?: number;
  step?: string;
  value?: any;
  section?: string;
  help?: string;
}
export interface Master {
  key: string;
  title: string;
  singular: string;
  code: string;
  name: string;
  description: string;
  icon: string;
  fields: Field[];
  columns: { key: string; label: string }[];
}
const text = (key: string, label: string, maxLength = 100, required = false): Field => ({
  key,
  label,
  maxLength,
  required,
});
const number = (
  key: string,
  label: string,
  min = 0,
  value: any = null,
  max = 2147483647,
): Field => ({ key, label, type: 'number', min, max, value });
const flag = (key: string, label: string, value = false): Field => ({
  key,
  label,
  type: 'checkbox',
  value,
});
const company: Field = { key: 'companyId', label: 'Company', type: 'select', required: true };
const address = [
  text('city', 'City'),
  text('state', 'State'),
  { ...text('country', 'Country', 100, true), value: 'India' },
  text('pinCode', 'PIN code', 20),
];
const base = (prefix: string): Field[] => [
  text(prefix + 'Code', 'Code', 50, true),
  text(prefix + 'Name', 'Name', 200, true),
];
const status = flag('isActive', 'Active record', true);
export const MASTERS: Master[] = [
  {
    key: 'companies',
    title: 'Companies',
    singular: 'Company',
    code: 'companyCode',
    name: 'companyName',
    icon: 'building',
    description: 'Manage legal entities and organisation-wide preferences.',
    fields: [
      ...base('company'),
      text('legalName', 'Legal name', 250),
      { ...text('registeredAddress', 'Registered address', 1000), type: 'textarea' },
      ...address,
      ...['PAN', 'TAN', 'GSTIN', 'PFRegistrationNumber', 'ESIRegistrationNumber'].map((key) =>
        text(
          key[0].toLowerCase() + key.slice(1),
          (
            {
              PAN: 'PAN',
              TAN: 'TAN',
              GSTIN: 'GSTIN',
              PFRegistrationNumber: 'PF registration number',
              ESIRegistrationNumber: 'ESI registration number',
            } as Row
          )[key],
          50,
        ),
      ),
      { ...text('payrollCurrency', 'Payroll currency', 3, true), value: 'INR' },
      { ...number('payrollDay', 'Payroll day', 1, null, 31), required: true },
      { ...number('salaryPaymentDay', 'Salary payment day', 1, null, 31), required: true },
      { ...text('logoUrl', 'Logo URL', 2048), type: 'url' },
      status,
    ],
    columns: [
      { key: 'state', label: 'State' },
      { key: 'country', label: 'Country' },
      { key: 'payrollCurrency', label: 'Currency' },
    ],
  },
  {
    key: 'branches',
    title: 'Branches',
    singular: 'Branch',
    code: 'branchCode',
    name: 'branchName',
    icon: 'branch',
    description: 'Organise your offices and their company relationships.',
    fields: [
      company,
      ...base('branch'),
      text('addressLine1', 'Address line 1', 500),
      text('addressLine2', 'Address line 2', 500),
      ...address,
      { ...text('email', 'Email', 254), type: 'email' },
      { ...text('phone', 'Phone', 30), type: 'tel' },
      flag('isHeadOffice', 'Head office'),
      status,
    ],
    columns: [
      { key: 'companyId', label: 'Company' },
      { key: 'city', label: 'City' },
      { key: 'isHeadOffice', label: 'Head office' },
    ],
  },
  {
    key: 'departments',
    title: 'Departments',
    singular: 'Department',
    code: 'departmentCode',
    name: 'departmentName',
    icon: 'hierarchy',
    description: 'Define functional teams and your reporting structure.',
    fields: [
      company,
      ...base('department'),
      { ...text('description', 'Description', 1000), type: 'textarea' },
      { key: 'parentDepartmentId', label: 'Parent department', type: 'select' },
      status,
    ],
    columns: [
      { key: 'companyId', label: 'Company' },
      { key: 'parentDepartmentId', label: 'Parent department' },
      { key: 'description', label: 'Description' },
    ],
  },
  {
    key: 'designations',
    title: 'Designations',
    singular: 'Designation',
    code: 'designationCode',
    name: 'designationName',
    icon: 'badge',
    description: 'Maintain job titles, grades and management roles.',
    fields: [
      company,
      ...base('designation'),
      { ...text('description', 'Description', 1000), type: 'textarea' },
      text('grade', 'Grade', 50),
      number('level', 'Level', -2147483648),
      flag('isManagerial', 'Managerial role'),
      status,
    ],
    columns: [
      { key: 'companyId', label: 'Company' },
      { key: 'grade', label: 'Grade' },
      { key: 'level', label: 'Level' },
    ],
  },
  {
    key: 'shifts',
    title: 'Shifts',
    singular: 'Shift',
    code: 'shiftCode',
    name: 'shiftName',
    icon: 'clock',
    description: 'Set working hours, grace periods and shift preferences.',
    fields: [
      company,
      ...base('shift'),
      { key: 'startTime', label: 'Start time', type: 'time', required: true },
      {
        key: 'endTime',
        label: 'End time',
        type: 'time',
        required: true,
        help: 'Earlier than start time means the following day.',
      },
      { ...number('gracePeriodMinutes', 'Grace period (minutes)', 0, 0), required: true },
      number('minimumHalfDayMinutes', 'Minimum half-day (minutes)'),
      number('minimumFullDayMinutes', 'Minimum full-day (minutes)'),
      flag('isNightShift', 'Night shift'),
      status,
    ],
    columns: [
      { key: 'companyId', label: 'Company' },
      { key: 'startTime', label: 'Start time' },
      { key: 'endTime', label: 'End time' },
      { key: 'gracePeriodMinutes', label: 'Grace (min)' },
    ],
  },
  {
    key: 'work-locations',
    title: 'Work locations',
    singular: 'Work location',
    code: 'locationCode',
    name: 'locationName',
    icon: 'pin',
    description: 'Manage office locations and geofence preferences.',
    fields: [
      company,
      { key: 'branchId', label: 'Branch', type: 'select', required: true },
      ...base('location'),
      { ...text('address', 'Address', 1000), type: 'textarea' },
      ...address,
      {
        ...number('latitude', 'Latitude', -90, null, 90),
        step: '0.0000001',
        help: 'Leave both coordinates blank until verified.',
      },
      { ...number('longitude', 'Longitude', -180, null, 180), step: '0.0000001' },
      { ...number('allowedRadiusMeters', 'Allowed radius (metres)', 1, 100), required: true },
      flag('isGeoFenceEnabled', 'Geofence enabled', true),
      status,
    ],
    columns: [
      { key: 'branchId', label: 'Branch' },
      { key: 'city', label: 'City' },
      { key: 'allowedRadiusMeters', label: 'Radius (m)' },
      { key: 'isGeoFenceEnabled', label: 'Geofence' },
    ],
  },
];
// System.Text.Json camel-cases initialisms as a word.
MASTERS[0].fields.forEach((f) => {
  f.key =
    (
      {
        pAN: 'pan',
        tAN: 'tan',
        gSTIN: 'gstin',
        pFRegistrationNumber: 'pfRegistrationNumber',
        eSIRegistrationNumber: 'esiRegistrationNumber',
      } as Row
    )[f.key] ?? f.key;
});
