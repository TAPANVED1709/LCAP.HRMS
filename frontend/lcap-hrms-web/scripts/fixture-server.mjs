// Development verification only. Never used by the Angular application or deployed.
import http from 'node:http';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { randomUUID } from 'node:crypto';
const root = path.resolve('dist/lcap-hrms-web/browser');
const companyId = 'ec8af472-df65-43d4-9abf-9378e553e455';
const branchId = 'a7cb358b-f1c0-42ab-b812-b93c5dccaaaa';
const base = {
  isActive: true,
  isDeleted: false,
  createdAt: '2026-09-07T00:00:00Z',
  createdBy: 'test-fixture',
  updatedAt: null,
  updatedBy: null,
};
const row = (value) => ({ ...base, id: randomUUID(), companyId, ...value });
const data = {
  companies: [
    row({
      id: companyId,
      companyCode: 'LCAP',
      companyName: 'LCAP',
      legalName: null,
      state: 'Bihar',
      country: 'India',
      payrollCurrency: 'INR',
      payrollDay: 1,
      salaryPaymentDay: 1,
      pan: null,
      tan: null,
      gstin: null,
    }),
  ],
  branches: [
    row({
      id: branchId,
      branchCode: 'PATNA-HO',
      branchName: 'Patna Head Office',
      city: 'Patna',
      state: 'Bihar',
      country: 'India',
      isHeadOffice: true,
    }),
  ],
  departments: [
    'HR|Human Resources',
    'FIN|Finance',
    'SALES|Sales',
    'OPS|Operations',
    'MGMT|Management',
  ].map((x) => {
    const [departmentCode, departmentName] = x.split('|');
    return row({ departmentCode, departmentName, description: null, parentDepartmentId: null });
  }),
  designations: [
    'EXEC|Executive',
    'SREXEC|Senior Executive',
    'TL|Team Leader',
    'MGR|Manager',
    'HRM|HR Manager',
    'PAYADMIN|Payroll Admin',
  ].map((x) => {
    const [designationCode, designationName] = x.split('|');
    return row({
      designationCode,
      designationName,
      grade: null,
      level: null,
      description: null,
      isManagerial: false,
    });
  }),
  shifts: [
    row({
      shiftCode: 'GENERAL',
      shiftName: 'General Shift',
      startTime: '09:30:00',
      endTime: '18:30:00',
      gracePeriodMinutes: 15,
      minimumHalfDayMinutes: null,
      minimumFullDayMinutes: null,
      isNightShift: false,
    }),
  ],
  'work-locations': [
    row({
      branchId,
      locationCode: 'PATNA-OFFICE',
      locationName: 'Patna Office',
      city: 'Patna',
      state: 'Bihar',
      country: 'India',
      latitude: null,
      longitude: null,
      allowedRadiusMeters: 100,
      isGeoFenceEnabled: true,
    }),
  ],
};
const codeKeys = {
  companies: 'companyCode',
  branches: 'branchCode',
  departments: 'departmentCode',
  designations: 'designationCode',
  shifts: 'shiftCode',
  'work-locations': 'locationCode',
};
http
  .createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    const send = (status, data, message = 'OK') => {
      res.writeHead(status, { 'Content-Type': 'application/json' });
      res.end(
        status === 204
          ? ''
          : JSON.stringify({
              success: status < 400,
              data,
              message,
              errors: [],
              traceId: 'fixture',
            }),
      );
    };
    try {
      if (url.pathname.startsWith('/api/')) {
        if (req.headers.authorization !== 'Bearer fixture-token')
          return send(401, null, 'Unauthorized');
        const [, , resource, id] = url.pathname.split('/');
        const rows = data[resource];
        if (!rows) return send(404, null);
        if (req.method === 'GET') {
          if (id)
            return send(
              200,
              rows.find((x) => x.id === id),
            );
          const skip = Number(url.searchParams.get('skip') || 0),
            take = Number(url.searchParams.get('take') || 100);
          return send(200, rows.filter((x) => !x.isDeleted).slice(skip, skip + take));
        }
        const existing = rows.find((x) => x.id === id);
        if (req.method === 'DELETE') {
          if (!existing) return send(404, null);
          existing.isDeleted = true;
          return send(204, null);
        }
        let raw = '';
        for await (const part of req) raw += part;
        const body = JSON.parse(raw);
        const key = codeKeys[resource];
        if (
          rows.some(
            (x) =>
              x.id !== id &&
              x[key] === body[key]?.trim().toUpperCase() &&
              x.companyId === body.companyId,
          )
        )
          return send(409, null, 'This code is already in use.');
        body[key] = body[key]?.trim().toUpperCase();
        if (resource === 'shifts' && body.endTime < body.startTime) body.isNightShift = true;
        if (req.method === 'PUT') {
          if (!existing) return send(404, null);
          Object.assign(existing, body);
          return send(200, existing);
        }
        const created = row(body);
        rows.push(created);
        return send(201, created);
      }
      const requested = path.resolve(root, '.' + decodeURIComponent(url.pathname));
      if (!requested.startsWith(root + path.sep) && requested !== root) {
        res.writeHead(403);
        return res.end();
      }
      let file = requested;
      try {
        if (path.extname(file) === '') file = path.join(root, 'index.html');
        const bytes = await readFile(file);
        const type =
          {
            '.html': 'text/html',
            '.js': 'text/javascript',
            '.css': 'text/css',
            '.ico': 'image/x-icon',
            '.svg': 'image/svg+xml',
          }[path.extname(file)] || 'application/octet-stream';
        res.writeHead(200, { 'Content-Type': type });
        res.end(bytes);
      } catch {
        res.writeHead(404);
        res.end();
      }
    } catch (e) {
      send(500, null, e.message);
    }
  })
  .listen(4300, '127.0.0.1', () =>
    console.log(
      'Isolated UI test fixture: http://127.0.0.1:4300. Token: fixture-token. No real database.',
    ),
  );
