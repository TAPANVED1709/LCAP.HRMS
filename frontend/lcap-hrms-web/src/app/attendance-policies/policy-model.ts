import type { Field, Row } from '../organisation/master-config';
export const triggerNames = ['', 'Next qualifying late', 'At threshold', 'Manual review'];
export const penaltyNames = ['', 'Half day', 'Full day', 'Fixed amount', 'Hourly', 'Custom'];
export const resetNames = ['', 'After penalty', 'On non-late attendance', 'Monthly', 'Never'];
export const policyOptions: Record<string, { value: string; label: string }[]> = {};
for (const [key, names] of Object.entries({
  penaltyTriggerMode: triggerNames,
  penaltyType: penaltyNames,
  resetMode: resetNames,
}))
  policyOptions[key] = names.slice(1).map((label, i) => ({ value: String(i + 1), label }));
export const policyFields: Field[] = [
  { key: 'companyId', label: 'Company', type: 'select', required: true, section: 'General' },
  { key: 'policyCode', label: 'Policy code', required: true, maxLength: 30, section: 'General' },
  { key: 'policyName', label: 'Policy name', required: true, maxLength: 200, section: 'General' },
  {
    key: 'description',
    label: 'Description',
    type: 'textarea',
    maxLength: 2000,
    section: 'General',
  },
  {
    key: 'gracePeriodMinutes',
    label: 'Grace period (minutes)',
    type: 'number',
    min: 0,
    max: 1440,
    required: true,
    value: 15,
    section: 'Timing',
  },
  {
    key: 'lateRuleEnabled',
    label: 'Enable late rule',
    type: 'checkbox',
    value: true,
    section: 'Late rules',
  },
  {
    key: 'consecutiveLateThreshold',
    label: 'Late threshold',
    type: 'number',
    min: 0,
    max: 10000,
    value: 3,
    required: true,
    section: 'Late rules',
  },
  {
    key: 'penaltyTriggerMode',
    label: 'Penalty trigger',
    type: 'select',
    value: 1,
    required: true,
    section: 'Late rules',
  },
  {
    key: 'penaltyType',
    label: 'Penalty type',
    type: 'select',
    value: 1,
    required: true,
    section: 'Penalty event',
  },
  {
    key: 'penaltyValue',
    label: 'Configured event value',
    type: 'number',
    min: 0.001,
    max: 999999999,
    step: '0.001',
    section: 'Penalty event',
    help: 'Metadata for later review. This screen does not calculate deductions.',
  },
  {
    key: 'resetMode',
    label: 'Reset mode',
    type: 'select',
    value: 1,
    required: true,
    section: 'Reset',
  },
  {
    key: 'effectiveFrom',
    label: 'Effective from',
    type: 'date',
    required: true,
    section: 'Effective period',
  },
  { key: 'effectiveTo', label: 'Effective to', type: 'date', section: 'Effective period' },
  {
    key: 'isDefault',
    label: 'Company default',
    type: 'checkbox',
    value: false,
    section: 'Effective period',
  },
  { key: 'isActive', label: 'Active', type: 'checkbox', value: true, section: 'Effective period' },
];
export function canManagePolicies(roles: string[]) {
  return roles.some((r) => r === 'HRAdmin' || r === 'SuperAdmin');
}
export function policyPayload(row: Row): Row {
  const p: Row = {};
  for (const f of policyFields) {
    let v = row[f.key];
    if (f.type === 'number' || (f.type === 'select' && f.key !== 'companyId'))
      v = v === '' || v == null ? null : Number(v);
    if (typeof v === 'string') v = v.trim() || null;
    p[f.key] = v;
  }
  if ([1, 2].includes(p['penaltyType'])) p['penaltyValue'] = null;
  return p;
}
export function policyError(p: Row) {
  if (
    !p['companyId'] ||
    !p['policyCode']?.trim() ||
    !p['policyName']?.trim() ||
    !p['effectiveFrom']
  )
    return 'Company, code, name and effective date are required.';
  if (
    !Number.isInteger(p['gracePeriodMinutes']) ||
    p['gracePeriodMinutes'] < 0 ||
    p['gracePeriodMinutes'] > 1440
  )
    return 'Grace must be a whole number between 0 and 1440.';
  if (
    !Number.isInteger(p['consecutiveLateThreshold']) ||
    p['consecutiveLateThreshold'] < 0 ||
    p['consecutiveLateThreshold'] > 10000 ||
    (p['lateRuleEnabled'] && p['consecutiveLateThreshold'] < 1)
  )
    return 'An enabled late rule requires a positive whole-number threshold.';
  if (
    p['effectiveFrom'] < '1900-01-01' ||
    (p['effectiveTo'] && p['effectiveTo'] < p['effectiveFrom'])
  )
    return 'Check the effective period.';
  if (
    ![1, 2, 3].includes(p['penaltyTriggerMode']) ||
    ![1, 2, 3, 4, 5].includes(p['penaltyType']) ||
    ![1, 2, 3, 4].includes(p['resetMode'])
  )
    return 'Select valid policy options.';
  if ([3, 4].includes(p['penaltyType']) && !(p['penaltyValue'] > 0))
    return 'This event type requires a positive configured value.';
  if (p['penaltyValue'] != null && (!(p['penaltyValue'] > 0) || p['penaltyValue'] > 999999999))
    return 'Configured value is outside the allowed range.';
  return '';
}
export interface Evaluation {
  isEvaluated: boolean;
  isLate: boolean;
  lateMinutes: number;
  gracePeriodMinutes: number;
  consecutiveLateCount: number;
  sequenceAfterEvaluation: number;
  threshold: number;
  thresholdReached: boolean;
  penaltyTriggered: boolean;
  nextLateTriggersPenalty: boolean;
  reasonCode?: string;
}
export function attendanceResult(e?: Evaluation | null) {
  return !e?.isEvaluated ? 'Not evaluated' : e.isLate ? 'Late' : 'Present';
}
export function sequenceText(e: Evaluation) {
  return e.consecutiveLateCount + ' of ' + e.threshold;
}
export function evaluationNotice(e: Evaluation) {
  return e.penaltyTriggered
    ? 'Attendance penalty event generated for payroll review.'
    : e.nextLateTriggersPenalty
      ? 'Your next qualifying late attendance may trigger a payroll-impact event under the configured attendance policy.'
      : '';
}
