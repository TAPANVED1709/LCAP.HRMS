import test from 'node:test';import assert from 'node:assert/strict';import {readFile,writeFile} from 'node:fs/promises';
const base='http://127.0.0.1:5093';const admin=(await readFile(new URL('../../artifacts/day03/access-token.txt',import.meta.url),'utf8')).trim();const evidence=[];
async function request(method,path,body,status=200,token=admin){const response=await fetch(base+path,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined});const raw=await response.text();assert.equal(response.status,status,method+' '+path+': '+raw);evidence.push({method,path,status});return raw?JSON.parse(raw).data:undefined;}
async function identity(role,companyId,employeeId){const r=await fetch(base+'/test/token',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({role,companyId,employeeId})});assert.equal(r.status,200);return r.text();}
const masters={};for(const name of ['companies','branches','departments','designations','shifts','work-locations'])masters[name]=await request('GET','/api/'+name);
const companyId=masters.companies[0].id,branchId=masters.branches[0].id;
assert.equal(masters['work-locations'][0].latitude,null);assert.equal(masters['work-locations'][0].longitude,null);
const location=await request('POST','/api/work-locations',{companyId,branchId,locationCode:'TEST-GEO',locationName:'Synthetic Test Office',latitude:0,longitude:0,allowedRadiusMeters:100,isGeoFenceEnabled:true,isActive:true},201);
const defaults={companyId,branchId,departmentId:masters.departments[0].id,designationId:masters.designations[0].id,shiftId:masters.shifts[0].id,workLocationId:location.id,firstName:'Synthetic',lastName:'Tester',mobileNumber:'0000000000',dateOfJoining:'2026-01-01',employmentType:5,employeeStatus:1,isActive:true};
async function employee(code,overrides={}){const row=await request('POST','/api/employees',{...defaults,employeeCode:code,...overrides},201);return {row,token:await identity('Employee',companyId,row.id)};}
const position=()=>({latitude:0,longitude:0,accuracyMeters:10,clientTimestamp:new Date().toISOString()});
let completed,main;
test('health, Swagger and real JWT authentication',async()=>{
 assert.equal((await fetch(base+'/api/health')).status,200);assert.equal((await fetch(base+'/swagger/index.html')).status,200);
 const doc=await (await fetch(base+'/swagger/v1/swagger.json')).json();for(const p of ['/api/attendance/check-in','/api/attendance/check-out','/api/attendance/me/today','/api/attendance/me','/api/attendance/{id}','/api/attendance'])assert.ok(doc.paths[p]);
 await request('POST','/api/attendance/check-in',position(),401,null);await request('POST','/api/attendance/check-in',position(),401,'invalid-token');
 await request('POST','/api/attendance/check-in',position(),403,await identity('Employee',companyId));
});
test('outside, inaccurate, missing, out of range and stale GPS are rejected',async()=>{
 main=await employee('TEST-ATTENDANCE');
 for(const invalid of [{latitude:.01},{accuracyMeters:101},{latitude:null},{longitude:null},{accuracyMeters:0},{latitude:91},{longitude:-181},{clientTimestamp:'2020-01-01T00:00:00Z'}])await request('POST','/api/attendance/check-in',{...position(),...invalid},400,main.token);
 assert.equal((await request('GET','/api/attendance/me/today',null,200,main.token)).record,null);
});
test('server stamps check-in and checkout, duplicates rejected, exact GPS never returned',async()=>{
 const start=Date.now();const row=await request('POST','/api/attendance/check-in',position(),201,main.token);
 assert.equal(row.timeZoneId,"Asia/Kolkata");assert.equal(row.withinGeofence,true);assert.equal(row.distanceMeters,0);assert.ok(Date.parse(row.checkInTime)>=start-1000);
 await request('POST','/api/attendance/check-in',position(),409,main.token);
 await request('POST','/api/attendance/check-out',{...position(),latitude:.01},400,main.token);
 completed=await request('POST','/api/attendance/check-out',position(),200,main.token);assert.equal(completed.attendanceId,row.attendanceId);assert.ok(completed.checkOutTime);
 await request('POST','/api/attendance/check-out',position(),409,main.token);
 await request('POST','/api/attendance/check-in',position(),409,main.token);
 const today=await request('GET','/api/attendance/me/today',null,200,main.token);assert.equal(today.record.attendanceId,row.attendanceId);
 const list=await request('GET','/api/attendance/me',null,200,main.token);assert.equal(list.length,1);
 for(const r of [row,completed,...list])for(const key of ['latitude','longitude','checkInLatitude','checkOutLongitude','pan','aadhaarNumber','bankAccountNumber','mobileNumber'])assert.ok(!(key in r));
});
test('SQL transaction serializes concurrent check-ins and check-outs',async()=>{
 const e=await employee('TEST-RACE');
 async function race(action,expected){const responses=await Promise.all(Array.from({length:4},()=>fetch(base+'/api/attendance/'+action,{method:'POST',headers:{Authorization:'Bearer '+e.token,'Content-Type':'application/json'},body:JSON.stringify(position())})));const statuses=responses.map(r=>r.status).sort();assert.deepEqual(statuses,expected);evidence.push({method:'POST',path:'/api/attendance/'+action,concurrent:4,statuses});}
 await race('check-in',[201,409,409,409]);await race('check-out',[200,409,409,409]);assert.equal((await request('GET','/api/attendance/me',null,200,e.token)).length,1);
});
test('no prior check-in, forged identity, tenant mismatch and read scope enforced',async()=>{
 const e=await employee('TEST-SECURITY');await request('POST','/api/attendance/check-out',position(),409,e.token);
 await request('POST','/api/attendance/check-in',{...position(),employeeId:main.row.id},400,e.token);
 await request('POST','/api/attendance/check-in',{...position(),companyId},400,e.token);
 await request('POST','/api/attendance/check-in',position(),403,await identity('Employee','11111111-1111-1111-1111-111111111111',e.row.id));
 await request('GET','/api/attendance/'+completed.attendanceId,null,403,e.token);await request('GET','/api/attendance',null,403,e.token);
 const foreign=await identity('HRAdmin','11111111-1111-1111-1111-111111111111');assert.deepEqual(await request('GET','/api/attendance',null,200,foreign),[]);await request('GET','/api/attendance/'+completed.attendanceId,null,403,foreign);
 const hr=await identity('HRAdmin',companyId);assert.ok((await request('GET','/api/attendance?employeeId='+main.row.id+'&branchId='+branchId,null,200,hr)).length===1);
 await request('GET','/api/attendance?take=0',null,400,hr);await request('GET','/api/attendance?status=99',null,400,hr);
});
test('inactive/deleted employee, inactive location and real office missing coordinates rejected',async()=>{
 const inactive=await employee('TEST-INACTIVE',{isActive:false});await request('POST','/api/attendance/check-in',position(),400,inactive.token);
 const deleted=await employee('TEST-DELETED');await request('DELETE','/api/employees/'+deleted.row.id,null,204);await request('POST','/api/attendance/check-in',position(),403,deleted.token);
 const unconfigured=await employee('TEST-PATNA',{workLocationId:masters['work-locations'][0].id});await request('POST','/api/attendance/check-in',position(),409,unconfigured.token);
 const unavailable=await request('POST','/api/work-locations',{companyId,branchId,locationCode:'TEST-INACTIVE',locationName:'Inactive Test',isActive:true},201);
 const assigned=await employee('TEST-LOCATION-INACTIVE',{workLocationId:unavailable.id});await request('PUT','/api/work-locations/'+unavailable.id,{...unavailable,isActive:false});await request('POST','/api/attendance/check-in',position(),409,assigned.token);
});
test('disabled geofence is explicit and still enforces GPS quality',async()=>{
 const disabled=await request('POST','/api/work-locations',{companyId,branchId,locationCode:'TEST-NOFENCE',locationName:'No Fence Test',isGeoFenceEnabled:false},201);
 const e=await employee('TEST-NOFENCE',{workLocationId:disabled.id});await request('POST','/api/attendance/check-in',{...position(),accuracyMeters:101},400,e.token);
 const row=await request('POST','/api/attendance/check-in',position(),201,e.token);assert.equal(row.withinGeofence,null);assert.equal(row.distanceMeters,null);await request('POST','/api/attendance/check-out',position(),200,e.token);
});
test('open attendance blocks reassignment; immutable history blocks location deletion',async()=>{
 const e=await employee('TEST-ASSIGNMENT');await request('POST','/api/attendance/check-in',position(),201,e.token);
 await request('PUT','/api/employees/'+e.row.id,{...defaults,employeeCode:'TEST-ASSIGNMENT',workLocationId:masters['work-locations'][0].id},409);
 await request('POST','/api/attendance/check-out',position(),200,e.token);await request('DELETE','/api/work-locations/'+location.id,null,409);
});
test('browser fixture and safe test evidence',async()=>{
 const e=await employee('TEST-BROWSER');await writeFile(new URL('../../artifacts/day03/browser-token.txt',import.meta.url),e.token);
 await writeFile(new URL('../../artifacts/day03/fixture-ids.json',import.meta.url),JSON.stringify({employeeId:e.row.id,companyId,branchId,locationId:location.id}));
 await writeFile(new URL('../../artifacts/day03/http-evidence.json',import.meta.url),JSON.stringify(evidence,null,2));
});

