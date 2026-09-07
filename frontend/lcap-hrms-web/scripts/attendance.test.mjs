import test from 'node:test';
import assert from 'node:assert/strict';
import ts from 'typescript';
import { readFile } from 'node:fs/promises';
const source = await readFile(
  new URL('../src/app/attendance/attendance-model.ts', import.meta.url),
  'utf8',
);
const { locate, attendanceState, captureAndSubmit } = await import(
  'data:text/javascript;base64,' +
    Buffer.from(
      ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
      }).outputText,
    ).toString('base64')
);
for (const [code, message] of [
  [1, /permission is required/],
  [2, /unavailable/],
  [3, /timed out/],
])
  test('geolocation error ' + code, async () => {
    await assert.rejects(
      locate({
        getCurrentPosition(ok, fail) {
          fail({ code });
        },
      }),
      message,
    );
  });
test('location is fresh, high accuracy, bounded and remains pending while locating', async () => {
  let success,
    settled = false;
  const pending = locate({
    getCurrentPosition(ok, fail, options) {
      success = ok;
      assert.deepEqual(options, { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 });
    },
  }).then((r) => {
    settled = true;
    return r;
  });
  await Promise.resolve();
  assert.equal(settled, false);
  success({ coords: { latitude: 0, longitude: 1, accuracy: 10 }, timestamp: 1000 });
  assert.deepEqual(await pending, {
    latitude: 0,
    longitude: 1,
    accuracyMeters: 10,
    clientTimestamp: '1970-01-01T00:00:01.000Z',
  });
});
test('today is not checked in without a record', () =>
  assert.equal(attendanceState(null), 'Not Checked In'));
test('successful check-in changes today status', () =>
  assert.equal(
    attendanceState({ checkInTime: '2026-09-07T04:00Z', checkOutTime: null }),
    'Checked In',
  ));
test('successful check-out completes today status', () =>
  assert.equal(
    attendanceState({ checkInTime: '2026-09-07T04:00Z', checkOutTime: '2026-09-07T13:00Z' }),
    'Completed',
  ));
test('unsupported location gives safe actionable message', async () =>
  assert.rejects(locate(undefined), /supported browser over HTTPS/));

const geo = {
  getCurrentPosition(ok) {
    ok({ coords: { latitude: 0, longitude: 0, accuracy: 10 }, timestamp: 1000 });
  },
};
for (const message of [
  'You are outside the permitted attendance area.',
  'Location accuracy is insufficient. Please enable precise location and try again.',
  'Attendance already exists for this attendance date.',
])
  test('API rejection preserved and loading cleared: ' + message, async () => {
    const phases = [];
    await assert.rejects(
      captureAndSubmit(
        geo,
        async () => {
          throw new Error(message);
        },
        (p) => phases.push(p),
      ),
      { message },
    );
    assert.deepEqual(phases, ['Locating…', 'Saving…', '']);
  });
test('successful submission shows locating then saving and clears busy state', async () => {
  const phases = [];
  const row = { attendanceId: '1', checkInTime: 'now', checkOutTime: null };
  assert.equal(
    await captureAndSubmit(
      geo,
      async (p) => {
        assert.equal(p.accuracyMeters, 10);
        return row;
      },
      (p) => phases.push(p),
    ),
    row,
  );
  assert.deepEqual(phases, ['Locating…', 'Saving…', '']);
});
