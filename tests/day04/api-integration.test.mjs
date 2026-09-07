import test from 'node:test';import assert from 'node:assert/strict';import {readFile,writeFile} from 'node:fs/promises';import {execFileSync} from 'node:child_process';
const base='http://127.0.0.1:5094',db=process.env.DAY04_DB||'LCAP_HRMS_Day04_20260908_Audit';assert.match(db,/^LCAP_HRMS_Day04_[A-Za-z0-9_]+$/);
const admin=(await readFile(new URL('../../artifacts/day04/access-token.txt',import.meta.url),'utf8')).trim(),evidence=[];
async function request(method,path,body,status=200,token=admin){const r=await fetch(base+path,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined});const raw=await r.text();assert.equal(r.status,status,method+' '+path+': '+raw);evidence.push({method,path,status});return raw?JSON.parse(raw).data:undefined;}
async function identity(role,companyId,employeeId){const r=await fetch(base+'/test/token',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({role,companyId,employeeId})});assert.equal(r.status,200);return r.text();}
async function clock(day,minute=18){const utc=`2026-09-${String(day).padStart(2,'0')}T04:${String(minute).padStart(2,'0')}:00Z`;const r=await fetch(base+'/test/clock',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({utc})});assert.equal(r.status,204);}
const masters={};for(const r of ['companies','branches','departments','designations','shifts','work-locations'])masters[r]=await request('GET','/api/'+r);const companyId=masters.companies[0].id,branchId=masters.branches[0].id;
await clock(8);const location=await request('POST','/api/work-locations',{companyId,branchId,locationCode:'TEST-DAY04-GEO',locationName:'Synthetic Day 4 Office',latitude:0,longitude:0,allowedRadiusMeters:100,isGeoFenceEnabled:true},201);
const defaults={companyId,branchId,departmentId:masters.departments[0].id,designationId:masters.designations[0].id,shiftId:masters.shifts[0].id,workLocationId:location.id,firstName:'Synthetic',lastName:'Late Tester',mobileNumber:'0000000000',dateOfJoining:'2026-01-01',employmentType:5,employeeStatus:1,isActive:true};
async function employee(code){const row=await request('POST','/api/employees',{...defaults,employeeCode:code},201);return {row,token:await identity('Employee',companyId,row.id)};}
const position={latitude:0,longitude:0,accuracyMeters:10};const policyBody={companyId,policyCode:'TEST-POLICY',policyName:'Test Attendance Policy',gracePeriodMinutes:15,lateRuleEnabled:true,consecutiveLateThreshold:3,penaltyTriggerMode:1,penaltyType:1,resetMode:1,effectiveFrom:'2026-09-08',isDefault:false,isActive:true};let main,third,fourth,seed;
test('health, Swagger, roles and scoped policy seed',async()=>{
 assert.equal((await fetch(base+'/api/health')).status,200);assert.equal((await fetch(base+'/swagger/index.html')).status,200);const spec=await (await fetch(base+'/swagger/v1/swagger.json')).json();for(const p of ['/api/attendance-policies','/api/attendance-policies/current','/api/attendance/evaluations','/api/attendance-penalties','/api/employees/{employeeId}/late-summary'])assert.ok(spec.paths[p]);
 await request('GET','/api/attendance-policies',null,401,null);seed=(await request('GET','/api/attendance-policies')).find(p=>p.policyCode==='LCAP-STANDARD');assert.equal(seed.penaltyValue,null);assert.equal(seed.gracePeriodMinutes,15);
 const foreign=await identity('HRAdmin','11111111-1111-1111-1111-111111111111');assert.deepEqual(await request('GET','/api/attendance-policies',null,200,foreign),[]);await request('GET','/api/attendance-policies/'+seed.id,null,403,foreign);
 await request('POST','/api/attendance-policies',policyBody,403,await identity('Employee',companyId));await request('POST','/api/attendance-policies',policyBody,403,await identity('Manager',companyId));
});
test('HR manages own policy, rejects duplicate and invalid data, soft deletes',async()=>{
 const hr=await identity('HRAdmin',companyId);const p=await request('POST','/api/attendance-policies',policyBody,201,hr);await request('POST','/api/attendance-policies',{...policyBody,policyCode:' test-policy '},409,hr);
 for(const values of [{gracePeriodMinutes:-1},{consecutiveLateThreshold:0},{effectiveFrom:null},{effectiveTo:'2026-09-01'},{policyName:''},{penaltyType:99},{penaltyValue:500}])await request('POST','/api/attendance-policies',{...policyBody,policyCode:'INVALID',...values},400,hr);
 await request('POST','/api/attendance-policies',{...policyBody,policyCode:'OVERLAP',isDefault:true},409,hr);
 const updated=await request('PUT','/api/attendance-policies/'+p.id,{...policyBody,isActive:false},200,hr);assert.equal(updated.revision,2);await request('DELETE','/api/attendance-policies/'+p.id,null,204,hr);await request('GET','/api/attendance-policies/'+p.id,null,404,hr);
 assert.equal(await request('GET',`/api/attendance-policies/current?companyId=${companyId}&date=2026-09-07`,null,200,hr),null);
 assert.equal((await request('GET',`/api/attendance-policies/current?companyId=${companyId}&date=2026-09-08`,null,200,hr)).id,seed.id);
});
test('realistic LCAP sequence generates pending event on fourth late and resets',async()=>{
 main=await employee('TEST-DAY04-LATE');const mins=[18,19,16,20,18];for(let i=0;i<5;i++){await clock(8+i,mins[i]);const row=await request('POST','/api/attendance/check-in',position,201,main.token);assert.equal(row.evaluation.isLate,true);assert.equal(row.evaluation.lateMinutes,mins[i]-15);assert.equal(row.evaluation.consecutiveLateCount,i===4?1:i+1);assert.equal(row.evaluation.penaltyTriggered,i===3);assert.equal(row.evaluation.thresholdReached,i===2||i===3);if(i===2)third=row;if(i===3)fourth=row;await request('POST','/api/attendance/check-out',position,200,main.token);}
 const events=await request('GET',`/api/employees/${main.row.id}/attendance-penalties`,null,200,main.token);assert.equal(events.length,1);assert.equal(events[0].status,1);assert.equal(events[0].attendanceRecordId,fourth.attendanceId);
 const summary=await request('GET',`/api/employees/${main.row.id}/late-summary`,null,200,main.token);assert.equal(summary.currentLateSequence,1);assert.equal(summary.threshold,3);
 await request('GET',`/api/employees/${main.row.id}/late-summary`,null,403,await identity('Employee',companyId,'22222222-2222-2222-2222-222222222222'));
 const foreign=await identity('HRAdmin','11111111-1111-1111-1111-111111111111');assert.deepEqual(await request('GET','/api/attendance/evaluations',null,200,foreign),[]);assert.deepEqual(await request('GET','/api/attendance-penalties',null,200,foreign),[]);await request('GET','/api/attendance-penalties/'+events[0].id,null,403,foreign);
 for(const row of [...await request('GET','/api/attendance/evaluations'),...events])for(const key of ['latitude','longitude','checkInLatitude','checkInLongitude','pan','aadhaarNumber','bankAccountNumber','penaltyValue'])assert.ok(!(key in row));
 const filtered=await request('GET',`/api/attendance?employeeId=${main.row.id}&lateOnly=true&penaltyOnly=true`);assert.equal(filtered.length,1);assert.equal(filtered[0].attendanceId,fourth.attendanceId);
});
test('concurrent fresh evaluation creates only one evaluation and one event',async()=>{
 const e=await employee('TEST-DAY04-CONCURRENT');let id;
 for(let day=8;day<=11;day++){
 const date=`2026-09-${day.toString().padStart(2,'0')}`;
 const sql=`SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; DECLARE @id uniqueidentifier=NEWID(); INSERT dbo.AttendanceRecords(Id,CompanyId,EmployeeId,WorkLocationId,ShiftId,AttendanceDate,TimeZoneId,CheckInTime,CheckOutTime,CheckInLatitude,CheckInLongitude,CheckInAccuracyMeters,Status,CheckInSource,CreatedAt,IsDeleted) SELECT @id,CompanyId,Id,WorkLocationId,ShiftId,'${date}','Asia/Kolkata','${date}T04:18:00+00:00','${date}T12:18:00+00:00',0,0,10,2,'Test',SYSUTCDATETIME(),0 FROM dbo.Employees WHERE Id='${e.row.id}'; SELECT CONVERT(varchar(36),@id);`;
 id=execFileSync('sqlcmd',['-S','localhost','-E','-C','-I','-b','-d',db,'-h','-1','-W','-Q',sql],{encoding:'utf8'}).trim();assert.match(id,/^[a-f0-9-]{36}$/i);
 const calls=day===11?8:1;const responses=await Promise.all(Array.from({length:calls},()=>fetch(base+'/test/evaluate/'+id,{method:'POST'})));assert.ok(responses.every(r=>r.status===204));
 }
 const rows=await request('GET','/api/attendance/evaluations?employeeId='+e.row.id);assert.equal(rows.length,4);const events=await request('GET','/api/attendance-penalties?employeeId='+e.row.id);assert.equal(events.length,1);await fetch(base+'/test/evaluate/'+id,{method:'POST'});assert.equal((await request('GET','/api/attendance-penalties?employeeId='+e.row.id)).length,1);
 evidence.push({concurrentFreshEvaluation:8,expectedEvaluations:1,expectedEvents:1});
});
test('policy edit does not rewrite historical snapshot or raw evidence',async()=>{
 const changed=await request('PUT','/api/attendance-policies/'+seed.id,{...seed,gracePeriodMinutes:30});assert.equal(changed.revision,2);
 const old=await request('GET',`/api/attendance/${fourth.attendanceId}/evaluation`,null,200,main.token);assert.equal(old.gracePeriodMinutes,15);assert.equal(old.policyRevision,1);assert.equal(old.attendancePolicyId,seed.id);
 await fetch(base+'/test/evaluate/'+fourth.attendanceId,{method:'POST'});const after=await request('GET',`/api/attendance/${fourth.attendanceId}/evaluation`,null,200,main.token);assert.deepEqual(after,old);
 await request('PUT','/api/attendance-policies/'+seed.id,{...seed,gracePeriodMinutes:15});
});
test('concurrent overlapping defaults are serialized',async()=>{
 const company=await request('POST','/api/companies',{companyCode:'TEST-DAY04-OTHER',companyName:'Synthetic Other',payrollDay:1,salaryPaymentDay:1},201);
 const responses=await Promise.all(['A','B'].map(code=>fetch(base+'/api/attendance-policies',{method:'POST',headers:{Authorization:'Bearer '+admin,'Content-Type':'application/json'},body:JSON.stringify({...policyBody,companyId:company.id,policyCode:code,isDefault:true})})));assert.deepEqual(responses.map(r=>r.status).sort(),[201,409]);
 await request('POST','/api/attendance-policies',{...policyBody,companyId:company.id,policyCode:'FOREIGN'},403,await identity('HRAdmin',companyId));
});
test('browser fixture shows threshold warning and subsequent pending event',async()=>{
 const e=await employee('TEST-DAY04-BROWSER');for(let d=8;d<=10;d++){await clock(d,18);const row=await request('POST','/api/attendance/check-in',position,201,e.token);if(d===10)assert.equal(row.evaluation.nextLateTriggersPenalty,true);await request('POST','/api/attendance/check-out',position,200,e.token);}
 await writeFile(new URL('../../artifacts/day04/browser-token.txt',import.meta.url),e.token);await writeFile(new URL('../../artifacts/day04/hr-token.txt',import.meta.url),await identity('HRAdmin',companyId));await writeFile(new URL('../../artifacts/day04/fixture-ids.json',import.meta.url),JSON.stringify({employeeId:e.row.id,companyId,branchId,locationId:location.id,policyId:seed.id}));await writeFile(new URL('../../artifacts/day04/http-evidence.json',import.meta.url),JSON.stringify(evidence,null,2));
});
