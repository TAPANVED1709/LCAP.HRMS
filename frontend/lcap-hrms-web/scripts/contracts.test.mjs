import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';
async function load(file) {
  const source = await readFile(new URL(file, import.meta.url), 'utf8');
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
  });
  return import('data:text/javascript;base64,' + Buffer.from(outputText).toString('base64'));
}
const { MASTERS } = await load('../src/app/organisation/master-config.ts');
const { requestBody, crossFieldError } = await load('../src/app/organisation/master-utils.ts');
const projects = {
  companies: ['Companies', 'Company'],
  branches: ['Branches', 'Branch'],
  departments: ['Departments', 'Department'],
  designations: ['Designations', 'Designation'],
  shifts: ['Shifts', 'Shift'],
  'work-locations': ['WorkLocations', 'WorkLocation'],
};
function camelCase(value) {
  return value.replace(/^[A-Z]+(?=[A-Z][a-z]|$)|^[A-Z]/, (x) => x.toLowerCase());
}
for (const master of MASTERS) {
  test(master.title + ' fields match the actual ASP.NET writable DTO', async () => {
    const [folder, name] = projects[master.key];
    const source = await readFile(
      new URL(
        '../../../backend/LCAP.HRMS.Application/' + folder + '/DTOs/' + name + 'WriteRequest.cs',
        import.meta.url,
      ),
      'utf8',
    );
    const properties = [
      ...source.matchAll(
        /public\s+(?:Guid|int|bool|string|TimeOnly|decimal)\??\s+(\w+)\s*\{\s*get;/g,
      ),
    ]
      .map((x) => camelCase(x[1]))
      .sort();
    assert.deepEqual(master.fields.map((x) => x.key).sort(), properties);
    assert.equal(new Set(master.fields.map((x) => x.key)).size, properties.length);
  });
}
test('company updates preserve optional tax identifiers without sending audit data', () => {
  const master = MASTERS[0];
  const response = {
    companyCode: 'LCAP',
    companyName: 'LCAP',
    pan: 'TEST-PAN',
    tan: 'TEST-TAN',
    gstin: 'TEST-GSTIN',
    pfRegistrationNumber: 'TEST-PF',
    esiRegistrationNumber: 'TEST-ESI',
    payrollDay: 7,
    salaryPaymentDay: 15,
    isActive: false,
    id: 'server-id',
    createdBy: 'server-user',
    isDeleted: true,
  };
  const body = requestBody(master, response);
  for (const key of [
    'pan',
    'tan',
    'gstin',
    'pfRegistrationNumber',
    'esiRegistrationNumber',
    'payrollDay',
    'salaryPaymentDay',
    'isActive',
  ])
    assert.equal(body[key], response[key]);
  for (const key of ['id', 'createdBy', 'isDeleted']) assert.equal(Object.hasOwn(body, key), false);
});
test('shift payload preserves midnight, null thresholds and seconds', () => {
  const master = MASTERS.find((x) => x.key === 'shifts');
  const body = requestBody(master, {
    startTime: '00:00',
    endTime: '08:00:30',
    gracePeriodMinutes: 0,
    isNightShift: false,
    isActive: true,
  });
  assert.equal(body.startTime, '00:00:00');
  assert.equal(body.endTime, '08:00:30');
  assert.equal(body.gracePeriodMinutes, 0);
  assert.equal(body.minimumHalfDayMinutes, null);
  assert.equal(body.isNightShift, false);
});
test('shift validation allows overnight and rejects equal times and inverted thresholds', () => {
  assert.equal(
    crossFieldError('shifts', {
      startTime: '22:00:00',
      endTime: '06:00:00',
      minimumHalfDayMinutes: null,
      minimumFullDayMinutes: null,
    }),
    '',
  );
  assert.match(crossFieldError('shifts', { startTime: '09:00:00', endTime: '09:00:00' }), /differ/);
  assert.match(
    crossFieldError('shifts', {
      startTime: '09:00:00',
      endTime: '18:00:00',
      minimumHalfDayMinutes: 500,
      minimumFullDayMinutes: 400,
    }),
    /exceed/,
  );
});
test('coordinates remain nullable and zero is a valid paired coordinate', () => {
  assert.equal(crossFieldError('work-locations', { latitude: null, longitude: null }), '');
  assert.equal(crossFieldError('work-locations', { latitude: 0, longitude: 0 }), '');
  assert.match(crossFieldError('work-locations', { latitude: 0, longitude: null }), /both/);
  assert.match(crossFieldError('work-locations', { latitude: 1.12345678, longitude: 0 }), /seven/);
  assert.equal(
    crossFieldError('work-locations', { latitude: 89.1234567, longitude: 179.1234567 }),
    '',
  );
});
test('blank optional fields become null and explicit false switches survive updates', () => {
  const master = MASTERS.find((x) => x.key === 'work-locations');
  const body = requestBody(master, {
    address: '  ',
    latitude: null,
    longitude: null,
    allowedRadiusMeters: 100,
    isGeoFenceEnabled: false,
    isActive: false,
  });
  assert.equal(body.address, null);
  assert.equal(body.latitude, null);
  assert.equal(body.longitude, null);
  assert.equal(body.isGeoFenceEnabled, false);
  assert.equal(body.isActive, false);
});
