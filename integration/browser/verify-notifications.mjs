// I01-S08 BR-04/05/06/08/09/15 UC-10 AC-05/09/14: actual owned Docker/Chrome journeys.
import { chromium } from 'playwright-core';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { safeFailure } from './diagnostics.mjs';
const root=new URL('../../',import.meta.url),output=new URL('.artifacts/browser-S08/',root);await mkdir(output,{recursive:true});
const env=Object.fromEntries((await readFile(new URL('.env',root),'utf8')).split(/\r?\n/).filter(x=>x.includes('=')).map(x=>[x.slice(0,x.indexOf('=')).replace(/^\uFEFF/,''),x.slice(x.indexOf('=')+1)]));
const base=`http://localhost:${env.WEB_PORT??8080}`,checks=[],network=[],errors=[],fixtures={};let failure,debugPage,brokerStopped=false,manager,restoreEmployee,customerId;
const check=(label,value)=>{checks.push({label,passed:!!value});if(!value)throw Error(label);console.log('Passed: '+label);};
function docker(args){return execFileSync('docker',args,{cwd:fileURLToPath(root),encoding:'utf8',stdio:['ignore','pipe','pipe'],windowsHide:true,timeout:60000});}
function sqlInt(query){const command=`SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d BrokerHubDocker -h -1 -W -Q "SET NOCOUNT ON; ${query}"`;const output=docker(['compose','exec','-T','sql','/bin/sh','-c',command]);const value=Number(output.trim());if(!Number.isInteger(value))throw Error('Expected bounded SQL count');return value;}
const browser=await chromium.launch({channel:'chrome',headless:true});
async function surface(){const context=await browser.newContext({viewport:{width:1360,height:1000},locale:'en-US',timezoneId:'Africa/Cairo'});await context.route('**/*',r=>new URL(r.request().url()).origin===base?r.continue():r.abort());const page=await context.newPage();debugPage=page;page.setDefaultTimeout(20000);page.on('pageerror',e=>errors.push(safeFailure(e)));page.on('response',r=>{const u=new URL(r.url());if(u.pathname.startsWith('/api/'))network.push({path:u.pathname,method:r.request().method(),status:r.status()});});return {context,page};}
async function login(s,email,password=env.SMOKE_PASSWORD){await s.page.goto(base+'/auth/login');await s.page.locator('#account-email').fill(email);await s.page.locator('#account-password').fill(password);await s.page.getByRole('button',{name:'Sign in',exact:true}).click();await s.page.waitForURL('**/auth/session');await s.page.getByText(email,{exact:true}).waitFor();}
async function api(s,path,method='GET',body,key){const headers={Origin:base};if(method!=='GET'){const pair=await(await s.context.request.get(base+'/api/session/antiforgery')).json();headers['X-CSRF-TOKEN']=pair.token;}if(key)headers['Idempotency-Key']=key;return s.context.request.fetch(base+path,{method,headers,...(body!==undefined?{data:body}:{})});}
async function read(s,path){const r=await api(s,path);if(r.status()!==200)throw Error(`GET ${path} status${r.status()}`);return r.json();}
async function waitFor(label,fn,seconds=70){const deadline=Date.now()+seconds*1000;while(Date.now()<deadline){if(await fn())return;await new Promise(r=>setTimeout(r,1000));}throw Error('Timed out: '+label);}
async function idle(p){await p.waitForFunction(()=>!document.querySelector('section[aria-busy="true"]'));await p.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));}
async function visit(s){debugPage=s.page;await s.page.goto(base+'/notifications');await s.page.locator('app-notifications-page section.account-panel').waitFor();await idle(s.page);}
async function list(s,n=1,size=100,filter=''){return read(s,`/api/notifications?page=${n}&pageSize=${size}${filter?'&readStatus='+filter:''}`);}
async function find(s,resource,kind){for(let n=1;n<=50;n++){const p=await list(s,n);const row=p.items.find(x=>x.resource.id===resource&&x.kind===kind);if(row)return row;if(n*p.pageSize>=p.totalCount)return null;}return null;}
async function create(s,unitId,start){const r=await api(s,'/api/viewings','POST',{unitId,proposedStartUtc:start},randomUUID());check('Actual Customer viewing request201 while channel state is observed',r.status()===201);return r.json();}
try{
 const customer=await surface(),employee=await surface(),employeeTwo=await surface(),admin=await surface();manager=await surface();
 await login(customer,'browser@brokerhub.test');await login(employee,'employee@brokerhub.test');await login(employeeTwo,'employee-two@brokerhub.test');await login(manager,'manager@brokerhub.test');await login(admin,env.ADMIN_EMAIL,env.ADMIN_PASSWORD);
 const profile=await read(customer,'/api/customers/me');customerId=fixtures.customerId=profile.id;fixtures.ownerAccountId=(await read(customer,'/api/accounts/me')).accountId;
 const current=await read(manager,'/api/crm/customers/'+customerId);restoreEmployee=current.assignedEmployeeId;fixtures.originalEmployeeId=restoreEmployee;
 const crm=JSON.parse(await readFile(new URL('.artifacts/browser-S04/result.json',root),'utf8'));const target=crm.fixtures.employeeIds.find(id=>id!==restoreEmployee);if(!target||!restoreEmployee)throw Error('Existing owned assigned CRM fixture required');fixtures.transferTarget=target;
 const staff=(await api(employee,'/api/crm/customers/'+customerId)).status()===200?employee:employeeTwo;
 const units=await read(customer,'/api/catalog/units');if(!units.items.length)throw Error('Current public Unit required');fixtures.unitId=units.items[0].id;
 check('Current messaging readiness succeeds over verified TLS',(await customer.context.request.get(base+'/readyz')).status()===200);
 check('Technical-admin-only own notification list is denied403',(await api(admin,'/api/notifications')).status()===403);
 await admin.page.goto(base+'/workspace');check('Technical admin navigation exposes no notification destination',await admin.page.getByRole('link',{name:'Notifications',exact:true}).count()===0);
 const first=await create(customer,fixtures.unitId,new Date(Date.now()+86400000).toISOString());fixtures.humanViewingId=first.id;
 await waitFor('human notification',async()=>!!await find(customer,first.id,'Viewing.Requested'));
 const human=await find(customer,first.id,'Viewing.Requested');fixtures.humanNotificationId=human.id;
 check('Actual human source creates safe Customer DTO8/resource5',Object.keys(human).length===8&&Object.keys(human.resource).length===5&&!JSON.stringify(human).match(/phone|email|notes|amount|payload/i));
 check('Foreign Employee cannot read another recipient notification',(await api(staff,'/api/notifications/'+human.id)).status()===404);
 // Stop only the new owned broker; source commits and actorless worker continue through actual elapsed time.
 docker(['compose','stop','rabbitmq']);brokerStopped=true;
 check('Readiness reports broker outage503 without stopping the API',(await customer.context.request.get(base+'/readyz')).status()===503);
 const expires=await create(customer,fixtures.unitId,new Date(Date.now()+15000).toISOString());fixtures.systemViewingId=expires.id;
 check('Business request remains durably readable while broker is stopped',(await read(customer,'/api/viewings/'+expires.id)).status==='PendingConfirmation');
 await waitFor('actual worker System expiry source',async()=>sqlInt(`SELECT COUNT(*) FROM [Reliability].[SystemOutboxMessages] WHERE [ResourceId]='${expires.id}' AND [Name]='Viewing.Expired'`)>0,55);
 check('Actual elapsed worker creates actorless System source during outage',sqlInt(`SELECT COUNT(*) FROM [Reliability].[SystemOutboxMessages] WHERE [ResourceId]='${expires.id}' AND [Name]='Viewing.Expired'`)===1);
 docker(['compose','up','-d','--wait','--wait-timeout','90','rabbitmq']);brokerStopped=false;
 await waitFor('BOTH automatic recovery',async()=>!!await find(customer,expires.id,'Viewing.Expired')&&!!await find(customer,expires.id,'Viewing.Requested'),100);
 const system=await find(customer,expires.id,'Viewing.Expired');fixtures.systemNotificationId=system.id;
 check('Actual human and System notifications recover with original business effects',!!await find(customer,expires.id,'Viewing.Requested')&&(await read(customer,'/api/viewings/'+expires.id)).status==='Expired');
 const ownList=await list(customer,1,20);await visit(customer);
 check('Real list/count UI renders current notifications',await customer.page.getByRole('region',{name:'My notification list',exact:true}).count()===1&&ownList.totalCount>0);
 await customer.page.getByRole('region',{name:'My notification list',exact:true}).locator('article').first().getByRole('button').click();await idle(customer.page);
 const opened=ownList.items[0];if(opened.readAtUtc)throw Error('Fresh unread owned notification required');
 const put=customer.page.waitForResponse(r=>new URL(r.url()).pathname===`/api/notifications/${opened.id}/read`&&r.request().method()==='PUT');
 await customer.page.getByRole('button',{name:'Mark notification as read',exact:true}).click();const marked=await put;await idle(customer.page);const receipt=await marked.json();
 check('Actual UI mark-read sends no body and returns original notification',marked.status()===200&&!marked.request().postData()&&receipt.notificationId===opened.id);
 const replay=await api(customer,`/api/notifications/${opened.id}/read`,'PUT');check('Natural idempotent read retains original first-read UTC',(await replay.json()).readAtUtc===receipt.readAtUtc);
 check('Read/unread filters reflect saved first-read state',(await list(customer,1,100,'read')).items.some(x=>x.id===opened.id)&&!(await list(customer,1,100,'unread')).items.some(x=>x.id===opened.id));
 for(const query of ['?readStatus=all','?ReadStatus=read','?page=1&page=2','?readStatus='])check('Strict notification query400 '+query,(await api(customer,'/api/notifications'+query)).status()===400);
 const noCsrf=await customer.context.request.put(base+`/api/notifications/${opened.id}/read`,{headers:{Origin:base}});check('Actual read write rejects missing CSRF400',noCsrf.status()===400);
 check('Actual read write rejects a JSON body400',(await api(customer,`/api/notifications/${opened.id}/read`,'PUT',{})).status()===400);
 const staffRow=await find(staff,expires.id,'Viewing.Expired');if(!staffRow)throw Error('Current assigned Employee recipient required');fixtures.formerNotificationId=staffRow.id;
 const rootBefore=await read(manager,'/api/crm/customers/'+customerId);check('Manager transfer succeeds without changing immutable recipient facts',(await api(manager,`/api/crm/customers/${customerId}/transfer`,'POST',{employeeId:target,version:rootBefore.version,reason:'Fictional notification privacy test'},randomUUID())).status()===200);
 check('Former Employee loses historical notification detail and mark-read',(await api(staff,'/api/notifications/'+staffRow.id)).status()===404&&(await api(staff,`/api/notifications/${staffRow.id}/read`,'PUT')).status()===404);
 check('Former Employee list excludes old Customer recipient facts',!(await list(staff)).items.some(x=>x.resource.customerId===customerId));
 const transferred=await read(manager,'/api/crm/customers/'+customerId);check('Original owned assignment restored after privacy test',(await api(manager,`/api/crm/customers/${customerId}/transfer`,'POST',{employeeId:restoreEmployee,version:transferred.version,reason:'Restore assignment after fictional notification test'},randomUUID())).status()===200);restoreEmployee=null;
 await visit(customer);await customer.page.setViewportSize({width:320,height:900});await customer.page.locator('#notification-filter').focus();await customer.page.keyboard.press('Tab');check('LTR320px notification layout and native keyboard stay usable',await customer.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth&&document.activeElement?.tagName==='BUTTON'));await customer.page.screenshot({path:fileURLToPath(new URL('notifications-mobile.png',output)),fullPage:false});
 check('Private notifications absent from SSR and browser persistent storage',!(await(await customer.context.request.get(base+'/notifications')).text()).includes(human.id)&&await customer.page.evaluate(()=>localStorage.length===0&&sessionStorage.length===0));
 await visit(customer);await api(customer,'/api/accounts/logout','POST');await customer.page.getByRole('button',{name:'Reload notifications',exact:true}).click();await idle(customer.page);check('Actual401 removes private notification list/count/details',await customer.page.getByRole('region',{name:'My notification list',exact:true}).count()===0&&await customer.page.getByRole('region',{name:'Notification details',exact:true}).count()===0);
 check('No uncaught browser exceptions',errors.length===0);
}catch(e){failure=safeFailure(e,[env.ADMIN_PASSWORD,env.SMOKE_PASSWORD,env.RABBIT_PASSWORD]);if(debugPage)await debugPage.screenshot({path:fileURLToPath(new URL('failure.png',output)),fullPage:true}).catch(()=>{});
}finally{
 if(brokerStopped){try{docker(['compose','up','-d','--wait','--wait-timeout','90','rabbitmq']);brokerStopped=false;}catch{failure=(failure??'')+'; owned broker restore failed';}}
 if(restoreEmployee&&manager&&customerId){try{const current=await read(manager,'/api/crm/customers/'+customerId);if(current.assignedEmployeeId!==restoreEmployee){const r=await api(manager,`/api/crm/customers/${customerId}/transfer`,'POST',{employeeId:restoreEmployee,version:current.version,reason:'Restore assignment after notification check'},randomUUID());if(r.status()!==200)throw Error('restore');}}catch{failure=(failure??'')+'; assignment restore failed';}}
 await browser.close();
}
const report={status:failure?'Failed':'Passed',checks:checks.length,error:failure,results:checks,fixtures,network,uncaughtErrors:errors};await writeFile(new URL('result.json',output),JSON.stringify(report,null,2));console.log(JSON.stringify({status:report.status,checks:checks.length,error:failure},null,2));if(failure)process.exitCode=1;
