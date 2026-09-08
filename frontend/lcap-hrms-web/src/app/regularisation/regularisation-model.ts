export const requestTypes = [
  '',
  'Missed check-in',
  'Missed check-out',
  'Incorrect check-in',
  'Incorrect check-out',
  'Incorrect both',
  'Attendance not recorded',
  'Other',
];
export const statuses = [
  '',
  'Draft',
  'Submitted',
  'Pending Manager Approval',
  'Approved',
  'Rejected',
  'Cancelled',
  'Applied',
];
export function requestFields(type: number) {
  return {
    checkIn: [1, 3, 5, 6, 7].includes(type),
    checkOut: [2, 4, 5, 6, 7].includes(type),
    bothRequired: [5, 6].includes(type),
  };
}
export function requestError(
  type: number,
  date: string,
  reason: string,
  input: string,
  output: string,
) {
  if (!date || !reason.trim()) return 'Attendance date and reason are required.';
  if (reason.trim().length > 2000) return 'Reason must be at most 2000 characters.';
  if (type < 1 || type > 7 || !Number.isInteger(type)) return 'Choose a request type.';
  const fields = requestFields(type);
  if (type !== 7 && ((fields.checkIn && !input) || (fields.checkOut && !output)))
    return 'Enter the proposed times for this request type.';
  if (input && output && output < input)
    return 'Check-out must follow check-in. For overnight work, choose the following date.';
  return '';
}
export function canReview(roles: string[]) {
  return roles.includes('Manager');
}
export function canReadRegister(roles: string[]) {
  return roles.includes('HRAdmin') || roles.includes('SuperAdmin');
}
export function canCancel(status: number) {
  return status === 3;
}
function localParts(date: Date, zone: string) {
  const p = new Intl.DateTimeFormat('en-CA', {
    timeZone: zone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(date);
  const v = (type: string) => p.find((x) => x.type === type)!.value;
  return (
    v('year') +
    '-' +
    v('month') +
    '-' +
    v('day') +
    'T' +
    v('hour') +
    ':' +
    v('minute') +
    ':' +
    v('second')
  );
}
// Interpret input in the saved attendance timezone, never the browser's local timezone.
export function localToUtc(value: string, zone: string): string | null {
  if (!value) return null;
  const target = value.length === 16 ? value + ':00' : value;
  const nominal = Date.parse(target + 'Z');
  if (!Number.isFinite(nominal)) throw new Error('Enter a valid proposed time.');
  let utc = nominal;
  for (let n = 0; n < 4; n++) {
    const shown = Date.parse(localParts(new Date(utc), zone) + 'Z');
    utc += nominal - shown;
  }
  if (
    localParts(new Date(utc), zone) !== target ||
    [-3600000, 3600000].some((delta) => localParts(new Date(utc + delta), zone) === target)
  )
    throw new Error('This local time is ambiguous or unavailable. Contact HR for timezone review.');
  return new Date(utc).toISOString();
}
export interface RequestRow {
  id: string;
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  reportingManagerId: string;
  managerName: string;
  attendanceDate: string;
  timeZoneId: string;
  requestType: number;
  status: number;
  originalCheckInTime: string | null;
  originalCheckOutTime: string | null;
  requestedCheckInTime: string | null;
  requestedCheckOutTime: string | null;
  baseEffectiveCheckInTime: string | null;
  baseEffectiveCheckOutTime: string | null;
  employeeReason: string;
  supportingNote: string | null;
  submittedAt: string;
  reviewedAt: string | null;
  reviewerRemarks: string | null;
  appliedAt: string | null;
  cancellationReason: string | null;
  policyImpactReviewRequired: boolean;
}
export interface EffectiveState {
  employeeId: string;
  attendanceDate: string;
  timeZoneId: string;
  attendanceRecordId: string | null;
  originalCheckInTime: string | null;
  originalCheckOutTime: string | null;
  effectiveCheckInTime: string | null;
  effectiveCheckOutTime: string | null;
  source: number;
  regularisationRequestId: string | null;
  correctionId: string | null;
  policyImpactReviewRequired: boolean;
  evaluation: {
    isEvaluated: boolean;
    isLate: boolean;
    lateMinutes: number;
    evaluationVersion: number;
    consecutiveLateCount: number;
  } | null;
}
