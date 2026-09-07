import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile,writeFile} from 'node:fs/promises';
const base='http://127.0.0.1:5091';
const token=(await readFile(new URL('../../artifacts/day01/access-token.txt',import.meta.url),'utf8')).trim();
const evidence=[];
async function request(method,path,body,expected=200,auth=true){
 const response=await fetch(base+path,{method,headers:{...(auth?{Authorization:'Bearer '+token}:{}),...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined});
 const raw=await response.text();assert.equal(response.status,expected,method+' '+path+': '+raw);
 evidence.push({method,path,status:response.status});
 return raw?JSON.parse(raw):null;
}
const lists={};
for(const resource of ['companies','branches','departments','designations','shifts','work-locations'])lists[resource]=(await request('GET','/api/'+resource)).data;
const companyId=lists.companies[0].id,branchId=lists.branches[0].id;
test('health, Swagger, JWT enforcement and CORS work on the real ASP.NET pipeline',async()=>{
 const health=await request('GET','/api/health',null,200,false);assert.equal(health.success,true);
 const swagger=await request('GET','/swagger/v1/swagger.json',null,200,false);
 assert.equal(Object.keys(swagger.paths).length,18);
 const ui=await fetch(base+'/swagger/index.html');assert.equal(ui.status,200);assert.match(await ui.text(),/Swagger UI/);
 await request('GET','/api/companies',null,401,false);
 const invalid=await fetch(base+'/api/companies',{headers:{Authorization:'Bearer invalid-jwt'}});assert.equal(invalid.status,401);
 const cors=await fetch(base+'/api/companies',{method:'OPTIONS',headers:{Origin:'http://localhost:4200','Access-Control-Request-Method':'POST','Access-Control-Request-Headers':'authorization,content-type'}});
 assert.equal(cors.status,204);assert.equal(cors.headers.get('access-control-allow-origin'),'http://localhost:4200');
});
test('all expected seeds load with unique codes and null legal identifiers/coordinates',()=>{
 assert.deepEqual(Object.values(lists).map(x=>x.length),[1,1,5,6,1,1]);
 const company=lists.companies[0];for(const field of ['pan','tan','gstin','pfRegistrationNumber','esiRegistrationNumber'])assert.equal(company[field],null);
 const location=lists['work-locations'][0];assert.equal(location.latitude,null);assert.equal(location.longitude,null);
 assert.equal(lists.shifts[0].startTime,'09:30:00');assert.equal(lists.shifts[0].endTime,'18:30:00');
});
const specs=[
 ['companies','companyCode','companyName',{payrollDay:7,salaryPaymentDay:15},null],
 ['branches','branchCode','branchName',{companyId},'/api/companies/'+companyId+'/branches'],
 ['departments','departmentCode','departmentName',{companyId},'/api/companies/'+companyId+'/departments'],
 ['designations','designationCode','designationName',{companyId},'/api/companies/'+companyId+'/designations'],
 ['shifts','shiftCode','shiftName',{companyId,startTime:'22:00:00',endTime:'06:00:00',gracePeriodMinutes:15},'/api/companies/'+companyId+'/shifts'],
 ['work-locations','locationCode','locationName',{companyId,branchId,latitude:null,longitude:null,allowedRadiusMeters:100},'/api/branches/'+branchId+'/work-locations']
];
for(const [resource,code,name,extra,nested] of specs){
 test(resource+': SQL-backed CRUD, duplicate rejection, validation, auditing and soft delete',async()=>{
  const route='/api/'+resource;const body={...extra,[code]:'DAY01-'+resource.toUpperCase(),[name]:'Day 1 integration '+resource,isActive:true};
  await request('POST',route,{},400);
  const created=(await request('POST',route,body,201)).data;assert.equal(created.createdBy,'day01-integration-tester');assert.equal(created.isDeleted,false);
  await request('POST',route,{...body,[code]:body[code].toLowerCase()},409);
  assert.equal((await request('GET',route+'/'+created.id)).data.id,created.id);
  assert.ok((await request('GET',route)).data.some(x=>x.id===created.id));
  if(nested)assert.ok((await request('GET',nested)).data.some(x=>x.id===created.id));
  if(resource==='shifts'){
   assert.equal(created.isNightShift,true);
   await request('PUT',route+'/'+created.id,{...body,gracePeriodMinutes:-1},400);
   await request('PUT',route+'/'+created.id,{...body,endTime:body.startTime},400);
  }
  if(resource==='work-locations'){
   await request('PUT',route+'/'+created.id,{...body,latitude:91,longitude:0},400);
   await request('PUT',route+'/'+created.id,{...body,latitude:0,longitude:null},400);
   await request('PUT',route+'/'+created.id,{...body,allowedRadiusMeters:0},400);
  }
  if(resource==='departments')await request('PUT',route+'/'+created.id,{...body,parentDepartmentId:created.id},400);
  const updated=(await request('PUT',route+'/'+created.id,{...body,[name]:'Updated '+resource,isActive:false})).data;
  assert.equal(updated.isActive,false);assert.equal(updated[name],'Updated '+resource);assert.equal(updated.createdAt,created.createdAt);assert.equal(updated.updatedBy,'day01-integration-tester');
  const active=(await request('PUT',route+'/'+created.id,{...body,isActive:true})).data;assert.equal(active.isActive,true);
  await request('DELETE',route+'/'+created.id,null,204);
  await request('GET',route+'/'+created.id,null,404);
  await request('DELETE',route+'/'+created.id,null,404);
  assert.ok(!(await request('GET',route)).data.some(x=>x.id===created.id));
  await request('POST',route,body,409);
 });
}
test('write machine-readable HTTP evidence',async()=>{await writeFile(new URL('../../artifacts/day01/http-evidence.json',import.meta.url),JSON.stringify(evidence,null,2));});
