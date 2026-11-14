import { safeFailure } from './diagnostics.mjs';
import {chromium} from 'playwright-core';
import {mkdir,readFile,writeFile} from 'node:fs/promises';
const root=new URL('../../',import.meta.url);
const env=Object.fromEntries((await readFile(new URL('.env',root),'utf8')).split(/\r?\n/).filter(line=>line.includes('=')).map(line=>[line.slice(0,line.indexOf('=')).replace(/^\uFEFF/,''),line.slice(line.indexOf('=')+1)]));
const base=`http://localhost:${env.WEB_PORT??8080}`;
const fixture=JSON.parse(await readFile(new URL('.artifacts/browser-S03/result.json',root),'utf8')).fixtures;
const checks=[];let failure;const browser=await chromium.launch({channel:'chrome',headless:true});
function check(label,value){checks.push({label,passed:!!value});if(!value)throw new Error(label);}
try{
 const context=await browser.newContext({locale:'en-US'});await context.route('**/*',route=>new URL(route.request().url()).origin===base?route.continue():route.abort());const page=await context.newPage();
 await page.goto(base+'/auth/login');await page.locator('#account-email').fill(env.ADMIN_EMAIL);await page.locator('#account-password').fill(env.ADMIN_PASSWORD);await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/auth/session');await page.getByText(env.ADMIN_EMAIL,{exact:true}).waitFor();
 const idle=()=>page.waitForFunction(()=>!document.querySelector('app-catalog-admin-page section[aria-busy="true"]'));
 async function act(path,method,name){const pending=page.waitForResponse(r=>new URL(r.url()).pathname===path&&r.request().method()===method);await page.getByRole('button',{name,exact:true}).click();const response=await pending;check(`${method} ${name} returns200`,response.status()===200);await idle();return response.json();}
 await page.goto(base+`/admin/developers?section=projects&id=${fixture.projectId}`);await page.locator('#field-name').waitFor();await idle();
 await page.locator('#field-name').fill('Edited fictional project '+fixture.prefix);
 const project=await act(`/api/projects/${fixture.projectId}`,'PUT','Save changes');check('Project update keeps immutable parent/code and exact Version',project.developerId===fixture.developerId&&project.code===fixture.prefix+'-P1'&&typeof project.version==='string');
 await act(`/api/projects/${fixture.projectId}/deactivate`,'POST','Deactivate record');check('Inactive Project hides its published unit',(await context.request.get(base+`/api/catalog/units/${fixture.unitId}`)).status()===404);
 await act(`/api/projects/${fixture.projectId}/reactivate`,'POST','Reactivate record');check('Project reactivation restores eligible public detail',(await context.request.get(base+`/api/catalog/units/${fixture.unitId}`)).status()===200);
 await page.goto(base+`/admin/developers?section=units&id=${fixture.unitId}`);await page.locator('#field-title').waitFor();await idle();
 const unpublished=await act(`/api/units/${fixture.unitId}/unpublish`,'POST','Unpublish listing');check('Unpublish preserves actual external availability',unpublished.unit.publicationStatus==='Unpublished'&&unpublished.unit.availabilityStatus==='Available');check('Unpublished detail/image both hidden',(await context.request.get(base+`/api/catalog/units/${fixture.unitId}`)).status()===404&&(await context.request.get(base+`/api/catalog/units/${fixture.unitId}/images/${fixture.imageId}/content`)).status()===404);
 const restored=await act(`/api/units/${fixture.unitId}/publish`,'POST','Publish listing');check('Explicit republish returns nested actual Unit with current eligibility',restored.unit.publicationStatus==='Published');
 await context.close();
}catch(error){failure=safeFailure(error);for(const secret of [env.ADMIN_PASSWORD,env.SMOKE_PASSWORD])if(secret)failure=failure.split(secret).join('[redacted]');}finally{await browser.close();}
await mkdir(new URL('.artifacts/browser-S03-catalog-actions/',root),{recursive:true});const report={status:failure?'Failed':'Passed',checks:checks.length,error:failure,results:checks};await writeFile(new URL('.artifacts/browser-S03-catalog-actions/result.json',root),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));if(failure)process.exitCode=1;
