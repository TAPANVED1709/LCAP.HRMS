import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';
const source = await readFile(
  new URL('../src/app/regularisation/regularisation-model.ts', import.meta.url),
  'utf8',
);
const m = await import(
  'data:text/javascript;base64,' +
    Buffer.from(
      ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
      }).outputText,
    ).toString('base64')
);
for (const [type, inside, outside] of [
  [1, true, false],
  [2, false, true],
  [3, true, false],
  [4, false, true],
  [5, true, true],
  [6, true, true],
  [7, true, true],
])
  test('dynamic request fields ' + type, () => {
    assert.equal(m.requestFields(type).checkIn, inside);
    assert.equal(m.requestFields(type).checkOut, outside);
  });
test('required reason', () =>
  assert.match(m.requestError(1, '2026-09-08', ' ', '2026-09-08T09:40', ''), /reason/));
test('required proposed time', () => assert.ok(m.requestError(1, '2026-09-08', 'Reason', '', '')));
test('both times required', () =>
  assert.ok(m.requestError(6, '2026-09-08', 'Reason', '2026-09-08T09:40', '')));
test('overnight explicit next date allowed', () =>
  assert.equal(
    m.requestError(6, '2026-09-08', 'Reason', '2026-09-08T22:00', '2026-09-09T06:00'),
    '',
  ));
test('backwards times rejected', () =>
  assert.ok(m.requestError(5, '2026-09-08', 'Reason', '2026-09-08T22:00', '2026-09-08T06:00')));
test('other may contain only a reason', () =>
  assert.equal(m.requestError(7, '2026-09-08', 'Reason', '', ''), ''));
test('Kolkata input is independent of browser timezone', () =>
  assert.equal(m.localToUtc('2026-09-08T09:40', 'Asia/Kolkata'), '2026-09-08T04:10:00.000Z'));
test('overnight conversion', () =>
  assert.equal(m.localToUtc('2026-09-09T06:10', 'Asia/Kolkata'), '2026-09-09T00:40:00.000Z'));
test('invalid DST gap is rejected', () =>
  assert.throws(() => m.localToUtc('2026-03-08T02:30', 'America/New_York')));
test('ambiguous DST time is rejected', () =>
  assert.throws(() => m.localToUtc('2026-11-01T01:30', 'America/New_York')));
test('cancel pending only', () => {
  assert.equal(m.canCancel(3), true);
  for (const s of [1, 2, 4, 5, 6, 7]) assert.equal(m.canCancel(s), false);
});
test('manager review role does not grant HR override', () => {
  assert.equal(m.canReview(['Manager']), true);
  for (const r of ['Employee', 'HRAdmin', 'SuperAdmin', 'PayrollAdmin'])
    assert.equal(m.canReview([r]), false);
});
test('HR register roles', () => {
  assert.equal(m.canReadRegister(['HRAdmin']), true);
  assert.equal(m.canReadRegister(['SuperAdmin']), true);
  assert.equal(m.canReadRegister(['Manager']), false);
});
test('all request outcomes have status labels', () => {
  for (const s of [3, 4, 5, 6, 7]) assert.ok(m.statuses[s]);
  assert.equal(m.statuses[3], 'Pending Manager Approval');
});
const html = await readFile(
  new URL('../src/app/regularisation/regularisations.html', import.meta.url),
  'utf8',
);
test('forms and history provide loading error and submit states', () => {
  for (const text of [
    'Loading regularisation data',
    'role="alert"',
    'Submitting…',
    'Original check-in',
    'Assigned manager:',
    'Submitted:',
  ])
    assert.ok(html.includes(text));
});
test('review requires reject/cancel reasons and displays audited remarks', () => {
  for (const text of [
    'Rejection remarks (required)',
    'Cancellation reason (required)',
    'Review remarks (optional)',
    'Review remarks:',
    'Policy impact review required',
  ])
    assert.ok(html.includes(text));
});
