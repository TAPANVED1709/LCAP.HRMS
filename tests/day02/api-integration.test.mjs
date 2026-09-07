import test from 'node:test';import assert from 'node:assert/strict';import {readFile,writeFile} from 'node:fs/promises';
const base='http://127.0.0.1:5092';const admin=(await readFile(new URL('../../artifacts/day02/access-token.txt',import.meta.url),'utf8')).trim();const evidence=[];
async function request(method,path,body,status=200,token=admin){const response=await fetch(base+path,{method,headers:{...(token?{Authorization:'Bearer '+token}:{}),...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined});const raw=await response.text();assert.equal(response.status,status,method+' '+path+': '+raw);evidence.push({method,path,status});return raw?JSON.parse(raw).data:undefined;}
async function identity(role,companyId,employeeId){const r=await fetch(base+'/test/token',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({role,companyId,employeeId})});assert.equal(r.status,200);return r.text();}
const masters={};for(const name of ['companies','branches','departments','designations','shifts','work-locations'])masters[name]=await request('GET','/api/'+name);
const defaults={companyId:masters.companies[0].id,branchId:masters.branches[0].id,departmentId:masters.departments[0].id,designationId:masters.designations[0].id,shiftId:masters.shifts[0].id,workLocationId:masters['work-locations'][0].id,firstName:'Test',lastName:'Employee',mobileNumber:'0000000000',dateOfJoining:'2026-01-01',employmentType:5,employeeStatus:1,isActive:true};
let manager,employee,third;const body=code=>({...defaults,employeeCode:code});
test('real JWT authentication, roles, health and employee OpenAPI',async()=>{
 await request('GET','/api/employees',null,401,null);await request('GET','/api/employees',null,401,'bad-token');
 const health=await fetch(base+'/api/health');assert.equal(health.status,200);assert.equal((await health.json()).success,true);
 const swagger=await fetch(base+'/swagger/v1/swagger.json');assert.equal(swagger.status,200);assert.ok((await swagger.json()).paths['/api/employees/{id}/reporting-chain']);
 const ui=await fetch(base+'/swagger/index.html');assert.equal(ui.status,200);
 await request('POST','/api/employees',body('DENIED'),403,await identity('HRUser',defaults.companyId));
 await request('GET','/api/employees',null,403,await identity('HRAdmin'));
});
test('HRAdmin creates and updates employee with correct audit actor',async()=>{
 const hr=await identity('HRAdmin',defaults.companyId);
 manager=await request('POST','/api/employees',{...body('TEST-MGR-001'),lastName:'Manager'},201,hr);
 employee=await request('POST','/api/employees',{...body('TEST-EMP-001'),reportingManagerId:manager.id},201,hr);
 assert.equal(employee.createdBy,'day02-role-test');assert.equal(employee.reportingManagerName,'Test Manager');
 employee=await request('PUT','/api/employees/'+employee.id,{...body('TEST-EMP-001'),reportingManagerId:manager.id,firstName:'Updated Test'},200,hr);assert.equal(employee.firstName,'Updated Test');
 await request('POST','/api/employees',body(' test-emp-001 '),409,hr);
});
test('manager direct reports and own-team access exclude unrelated employees',async()=>{
 const token=await identity('Manager',defaults.companyId,manager.id);
 assert.deepEqual((await request('GET','/api/employees/'+manager.id+'/direct-reports',null,200,token)).map(e=>e.id),[employee.id]);
 assert.deepEqual((await request('GET','/api/employees/my-team',null,200,token)).map(e=>e.id),[employee.id]);
 await request('GET','/api/employees/'+employee.id+'/direct-reports',null,403,token);
 const self=await identity('Employee',defaults.companyId,employee.id);
 await request('GET','/api/employees/'+employee.id,null,200,self);await request('GET','/api/employees/'+manager.id,null,403,self);
 await request('PUT','/api/employees/'+employee.id,body('TEST-EMP-001'),403,self);
});
test('self, two and three-person cycles rejected; valid reporting chain accepted',async()=>{
 await request('PUT','/api/employees/'+manager.id,{...body('TEST-MGR-001'),reportingManagerId:manager.id},409);
 await request('PUT','/api/employees/'+manager.id,{...body('TEST-MGR-001'),reportingManagerId:employee.id},409);
 third=await request('POST','/api/employees',{...body('TEST-THIRD'),reportingManagerId:employee.id},201);
 await request('PUT','/api/employees/'+manager.id,{...body('TEST-MGR-001'),reportingManagerId:third.id},409);
 assert.deepEqual((await request('GET','/api/employees/'+third.id+'/reporting-chain')).map(e=>e.id),[employee.id,manager.id]);
});
test('concurrent inverse reporting changes cannot create a cycle',async()=>{
 const a=await request('POST','/api/employees',body('TEST-RACE-A'),201),b=await request('POST','/api/employees',body('TEST-RACE-B'),201);
 const responses=await Promise.all([[a,b,'TEST-RACE-A'],[b,a,'TEST-RACE-B']].map(async([e,m,code])=>fetch(base+'/api/employees/'+e.id,{method:'PUT',headers:{Authorization:'Bearer '+admin,'Content-Type':'application/json'},body:JSON.stringify({...body(code),reportingManagerId:m.id})})));
 assert.deepEqual(responses.map(r=>r.status).sort(),[200,409]);
 for(const e of [a,b])await request('GET','/api/employees/'+e.id+'/reporting-chain');
});
test('invalid identifiers and employment dates return safe validation errors',async()=>{
 for(const values of [{pan:'invalid'},{aadhaarNumber:'123'},{ifscCode:'invalid'},{dateOfBirth:'2099-01-01'},{dateOfJoining:null},{dateOfConfirmation:'2025-01-01'},{lastWorkingDate:'2025-01-01'},{mobileNumber:'invalid'},{officialEmail:'invalid'},{employeeStatus:999}])await request('POST','/api/employees',{...body('TEST-INVALID'),...values},400);
});
test('company ownership, branch/location ownership and code scope enforced',async()=>{
 const company=await request('POST','/api/companies',{companyCode:'TEST-OTHER',companyName:'Test Other',payrollDay:1,salaryPaymentDay:1},201);
 const branch=await request('POST','/api/branches',{companyId:company.id,branchCode:'TEST-OTHER',branchName:'Test Other'},201);
 const department=await request('POST','/api/departments',{companyId:company.id,departmentCode:'TEST-OTHER',departmentName:'Test Other'},201);
 const designation=await request('POST','/api/designations',{companyId:company.id,designationCode:'TEST-OTHER',designationName:'Test Other'},201);
 const shift=await request('POST','/api/shifts',{companyId:company.id,shiftCode:'TEST-OTHER',shiftName:'Test Other',startTime:'09:00:00',endTime:'17:00:00'},201);
 const location=await request('POST','/api/work-locations',{companyId:company.id,branchId:branch.id,locationCode:'TEST-OTHER',locationName:'Test Other'},201);
 for(const values of [{branchId:branch.id},{departmentId:department.id},{designationId:designation.id},{shiftId:shift.id},{workLocationId:location.id}])await request('POST','/api/employees',{...body('TEST-BAD'),...values},400);
 const localBranch=await request('POST','/api/branches',{companyId:defaults.companyId,branchCode:'TEST-LOCAL',branchName:'Test Local'},201);
 await request('POST','/api/employees',{...body('TEST-BAD'),branchId:localBranch.id},400);
 const foreign=await request('POST','/api/employees',{...body('TEST-EMP-001'),companyId:company.id,branchId:branch.id,departmentId:department.id,designationId:designation.id,shiftId:shift.id,workLocationId:location.id},201);
 await request('GET','/api/employees/'+foreign.id,null,403,await identity('HRAdmin',defaults.companyId));
 const scoped=await identity('HRAdmin',defaults.companyId);
 for(const route of ['/api/employees','/api/employees/lookup','/api/companies/'+defaults.companyId+'/employees']){
  const rows=await request('GET',route,null,200,scoped);
  assert.ok(rows.length>0);assert.ok(rows.every(e=>e.companyId===defaults.companyId));assert.ok(!rows.some(e=>e.id===foreign.id));
 }
 await request('GET','/api/companies/'+company.id+'/employees',null,403,scoped);
 await request('PUT','/api/employees/'+foreign.id,{...body('TEST-EMP-001'),companyId:company.id,branchId:branch.id,departmentId:department.id,designationId:designation.id,shiftId:shift.id,workLocationId:location.id},403,scoped);
 assert.deepEqual(await request('GET','/api/branches/'+branch.id+'/employees',null,200,scoped),[]);
 await request('POST','/api/employees',{...body('TEST-BAD'),reportingManagerId:foreign.id},400);
 await request('DELETE','/api/shifts/'+shift.id,null,409);
});
test('list omits identifiers and scoped detail redacts for HRUser, Manager and Employee',async()=>{
 // Only synthetic format data in a disposable test database; never migration seed data.
 const sensitive={...body('TEST-SENSITIVE'),pan:'ABCDE1234F',aadhaarNumber:'000000000000',bankAccountNumber:'000000000000',reportingManagerId:manager.id};
 const row=await request('POST','/api/employees',sensitive,201);
 const list=await request('GET','/api/employees');for(const e of list)for(const key of ['pan','aadhaarNumber','bankAccountNumber','uan','esicNumber'])assert.ok(!(key in e));
 for(const role of ['HRUser','Manager','Employee']){const e=await request('GET','/api/employees/'+row.id,null,200,await identity(role,defaults.companyId,role==='Manager'?manager.id:row.id));assert.equal(e.pan,null);assert.equal(e.bankAccountNumber,null);assert.equal(e.canViewSensitive,false);}
 const payroll=await request('GET','/api/employees/'+row.id,null,200,await identity('PayrollAdmin',defaults.companyId));assert.equal(payroll.pan,sensitive.pan);
 await request('DELETE','/api/employees/'+row.id,null,204);
});
test('employee soft delete hides row, reserves code and nested lists work',async()=>{
 for(const [resource,id]of [['companies',defaults.companyId],['branches',defaults.branchId],['departments',defaults.departmentId]])assert.ok((await request('GET',`/api/${resource}/${id}/employees`)).some(e=>e.id===employee.id));
 await request('DELETE','/api/employees/'+third.id,null,204);await request('GET','/api/employees/'+third.id,null,404);
 await request('POST','/api/employees',body('TEST-THIRD'),409);
 const row=await request('POST','/api/employees',body('TEST-DELETE'),201);await request('DELETE','/api/employees/'+row.id,null,204);
 await request('GET','/api/employees/'+row.id,null,404);assert.ok(!(await request('GET','/api/employees')).some(e=>e.id===row.id));
});
test('write safe HTTP evidence and browser fixture IDs',async()=>{
 await writeFile(new URL('../../artifacts/day02/http-evidence.json',import.meta.url),JSON.stringify(evidence,null,2));
 await writeFile(new URL('../../artifacts/day02/fixture-ids.json',import.meta.url),JSON.stringify({managerId:manager.id,employeeId:employee.id,...defaults},null,2));
});
