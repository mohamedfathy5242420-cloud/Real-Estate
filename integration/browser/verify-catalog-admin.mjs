import { safeFailure } from './diagnostics.mjs';
import { chromium } from 'playwright-core';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { deflateSync } from 'node:zlib';

const root=new URL('../../',import.meta.url);
const env=Object.fromEntries((await readFile(new URL('.env',root),'utf8')).split(/\r?\n/).filter(line=>line.includes('=')).map(line=>[line.slice(0,line.indexOf('=')).replace(/^\uFEFF/,''),line.slice(line.indexOf('=')+1)]));
const base=`http://localhost:${env.WEB_PORT??8080}`;
const output=new URL('.artifacts/browser-S03/',root);await mkdir(output,{recursive:true});
const prefix='I03-'+Date.now().toString(36).toUpperCase();
const checks=[],network=[],errors=[];const fixtures={prefix};let failure;let debugPage;let failureState;
function check(label,value){checks.push({label,passed:!!value});if(!value)throw new Error(`Check failed: ${label}`);}
const browser=await chromium.launch({channel:'chrome',headless:true});
const version=browser.version();
async function createContext(){const context=await browser.newContext({viewport:{width:1440,height:1000},locale:'en-US'});await context.route('**/*',route=>new URL(route.request().url()).origin===base?route.continue():route.abort());const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));page.on('response',response=>{const url=new URL(response.url());if(url.pathname.startsWith('/api/'))network.push({path:url.pathname,method:response.request().method(),status:response.status()});});return {context,page};}
async function login(page,email,password){await page.goto(base+'/auth/login');await page.locator('#account-email').fill(email);await page.locator('#account-password').fill(password);await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/auth/session');await page.getByText(email,{exact:true}).waitFor();}
async function idle(page){await page.waitForFunction(()=>!document.querySelector('app-catalog-admin-page section[aria-busy="true"]'));}
async function section(page,kind,id){await page.goto(base+`/admin/developers?section=${kind}`+(id?`&id=${id}`:''));await page.locator('app-catalog-admin-page').waitFor();await page.locator('#field-'+(kind==='agreements'?'agreementNumber':'code')).waitFor();await idle(page);}
async function fill(page,values){for(const [name,value] of Object.entries(values)){const field=page.locator('#field-'+name);if(await field.evaluate(el=>el.tagName==='SELECT'))await field.selectOption(value);else await field.fill(value);}}
async function mutation(page,path,method,button,status=200){const pending=page.waitForResponse(response=>new URL(response.url()).pathname===path&&response.request().method()===method);await page.getByRole('button',{name:button,exact:true}).click();const response=await pending;check(`${method} ${path.replace(/[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}/gi,':id')} actual${status}`,response.status()===status);let data;try{data=await response.json();}catch{data=null;}await idle(page);return data;}
async function csrf(context){return (await (await context.request.get(base+'/api/session/antiforgery')).json()).token;}
async function direct(context,path,method,body){return context.request.fetch(base+path,{method,headers:{Origin:base,'X-CSRF-TOKEN':await csrf(context)},data:body});}
async function developer(page,code){await section(page,'developers');await fill(page,{code,name:'Fictional developer '+code,legalName:'Fictional test data only',externalContactEmail:'private-catalog-contact@brokerhub.test',externalContactName:'Fictional contact'});return mutation(page,'/api/developers','POST','Create record',201);}
async function project(page,owner,code){await section(page,'projects');await fill(page,{developerId:owner,code,name:'Fictional project '+code,area:'Test area '+prefix,address:'Fictional test address',timeZoneId:'Africa/Cairo'});return mutation(page,'/api/projects','POST','Create record',201);}
async function unit(page,parent,code,title,price='9999999999999999.99',expected=201){await section(page,'units');await fill(page,{projectId:parent,code,title,description:'Fictional data, not a real property offer',unitType:'Apartment',areaSqm:'150.25',bedrooms:'2',bathrooms:'1',listPrice:price,currency:'EGP'});return mutation(page,'/api/units','POST','Create record',expected);}
async function agreement(page,owner,number,scope,selected=[],override=null){await section(page,'agreements');await fill(page,{developerId:owner,agreementNumber:number,scopeType:scope,startsOn:`${new Date().getUTCFullYear()-1}-01-01`,endsOn:`${new Date().getUTCFullYear()+1}-12-31`,protectionPeriodDays:'90'});for(const id of selected){const checkbox=page.locator('input[type="checkbox"]').filter({visible:true});await page.locator('label.checkbox').filter({hasText:id.code}).getByRole('checkbox').check();}await page.locator('#rule-value-0').fill('2.5001');await page.locator('#rule-currency-0').fill('EGP');if(override){await page.getByRole('button',{name:'Add commission rule',exact:true}).click();await page.locator('#rule-project-1').selectOption(override);await page.locator('#rule-type-1').selectOption('FixedAmount');await page.locator('label[for="rule-value-1"]',{hasText:'Fixed amount'}).waitFor();await page.locator('#rule-value-1').fill('1234.56');await page.locator('#rule-currency-1').fill('EGP');}return mutation(page,'/api/marketing-agreements','POST','Create record',201);}
function png(){const crc=bytes=>{let value=0xffffffff;for(const byte of bytes){value^=byte;for(let bit=0;bit<8;bit++)value=(value>>>1)^((value&1)?0xedb88320:0);}return (value^0xffffffff)>>>0;};const chunk=(name,data)=>{const type=Buffer.from(name),size=Buffer.alloc(4),sum=Buffer.alloc(4);size.writeUInt32BE(data.length);sum.writeUInt32BE(crc(Buffer.concat([type,data])));return Buffer.concat([size,type,data,sum]);};const header=Buffer.alloc(13);header.writeUInt32BE(16,0);header.writeUInt32BE(16,4);header[8]=8;header[9]=2;const rows=Buffer.alloc(16*(1+16*3));for(let y=0;y<16;y++)for(let x=0;x<16;x++){const index=y*49+1+x*3;rows[index]=42;rows[index+1]=115;rows[index+2]=93;}return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',header),chunk('IDAT',deflateSync(rows)),chunk('IEND',Buffer.alloc(0))]);}
try{
 const admin=await createContext();const {page,context}=admin;debugPage=page;
 await login(page,env.ADMIN_EMAIL,env.ADMIN_PASSWORD);
 const dev=await developer(page,prefix+'-A');fixtures.developerId=dev.id;
 check('Created Developer has no automatic account and an exact string Version',typeof dev.version==='string'&&!('accountId' in dev));
 await fill(page,{name:'Updated fictional developer '+prefix});
 const devEdited=await mutation(page,`/api/developers/${dev.id}`,'PUT','Save changes');
 check('Developer code is immutable in edit form',await page.locator('#field-code').isDisabled());
 await mutation(page,`/api/developers/${dev.id}/deactivate`,'POST','Deactivate record');
 await mutation(page,`/api/developers/${dev.id}/reactivate`,'POST','Reactivate record');
 const proj=await project(page,dev.id,prefix+'-P1');fixtures.projectId=proj.id;
 const created=await unit(page,proj.id,'U1','Fictional unit '+prefix);fixtures.unitId=created.id;
 check('Unit maximum price and area round-trip as exact strings',created.listPrice==='9999999999999999.99'&&created.areaSqm==='150.25'&&typeof created.version==='string');
 await mutation(page,`/api/units/${created.id}/publish`,'POST','Publish listing',409);
 check('Eligibility conflict disables blind resubmission until explicit reload',await page.getByRole('button',{name:'Save changes',exact:true}).isDisabled());
 const a=await agreement(page,dev.id,prefix+'-ALL','AllDeveloperProjects',[],proj.id);fixtures.agreementId=a.id;
 await fill(page,{protectionPeriodDays:'0'});
 const draft=await mutation(page,`/api/marketing-agreements/${a.id}`,'PUT','Save changes');
 check('Draft edit retains precise commission default and project override',draft.protectionPeriodDays===0&&draft.commissionRules.some(rule=>rule.percentage==='2.5001')&&draft.commissionRules.some(rule=>rule.fixedAmount==='1234.56'));
 await mutation(page,`/api/marketing-agreements/${a.id}/activate`,'POST','Activate agreement');
 check('Active agreement is displayed read-only with no false edit action',await page.getByRole('button',{name:'Save changes',exact:true}).count()===0&&await page.locator('#field-agreementNumber').isDisabled());
 const overlap=await agreement(page,dev.id,prefix+'-OVERLAP','SelectedProjects',[proj]);
 await mutation(page,`/api/marketing-agreements/${overlap.id}/activate`,'POST','Activate agreement',409);
 check('Overlapping Active scope is rejected without fake activation',await page.getByRole('alert').innerText().then(text=>text.includes('Conflict')));

 await section(page,'units',created.id);
 const published=await mutation(page,`/api/units/${created.id}/publish`,'POST','Publish listing');
 check('Publication uses its nested Unit response and does not invent external availability',published.unit.id===created.id&&published.unit.publicationStatus==='Published'&&published.unit.availabilityStatus!=='Available');
 const unconfirmed=await (await context.request.get(base+`/api/catalog/units/${created.id}`)).json();
 check('Published public unit retains unconfirmed freshness and external-confirmation requirement',unconfirmed.availability.freshness==='unconfirmed'&&unconfirmed.availability.requiresExternalConfirmation===true);
 await page.locator('#confirmation-time').fill(new Date(Date.now()-120000).toISOString());
 await page.locator('#confirmation-source').fill('Fictional verification source');await page.locator('#confirmation-notes').fill('private-confirmation-note-sentinel');await page.locator('#confirmation-availability').selectOption('Unavailable');
 const observed=await mutation(page,`/api/units/${created.id}/market-confirmations`,'POST','Record developer confirmation',201);
 check('External unavailability does not silently unpublish the ad',observed.unit.availabilityStatus==='Unavailable'&&observed.unit.publicationStatus==='Published');
 await page.locator('#confirmation-time').fill(new Date(Date.now()-60000).toISOString());await page.locator('#confirmation-source').fill('Newer fictional source');await page.locator('#confirmation-price').fill('9999999999999999.99');await page.locator('#confirmation-currency').fill('EGP');await page.locator('#confirmation-availability').selectOption('Available');
 await mutation(page,`/api/units/${created.id}/market-confirmations`,'POST','Record developer confirmation',201);
 check('Actual confirmation history displays private notes only in administration',(await page.locator('app-catalog-admin-page').innerText()).includes('private-confirmation-note-sentinel'));
 const chooserEvent=page.waitForEvent('filechooser');await page.getByRole('button',{name:'Select image file',exact:true}).click();await (await chooserEvent).setFiles({name:'fictional-color-square.png',mimeType:'image/png',buffer:png()});await page.locator('#image-alt').fill('Fictional color test sample, not a property photograph');await page.locator('#image-order').fill('0');
 await mutation(page,`/api/units/${created.id}/images`,'POST','Upload image',201);
 await page.locator('.image-grid img').waitFor();check('Linux decoder/private image storage works in actual browser',await page.locator('.image-grid img').evaluate(image=>image.complete&&image.naturalWidth===16));
 const imageList=await (await context.request.get(base+`/api/units/${created.id}/images`)).json();fixtures.imageId=imageList.images[0].id;
 await page.getByRole('button',{name:'Edit image description and order',exact:true}).first().click();await page.locator('#image-alt').fill('Updated alternative text for fictional test image');await page.locator('#image-order').fill('2');
 await mutation(page,`/api/units/${created.id}/images/${fixtures.imageId}`,'PUT','Save image details');
 check('No unsupported delete or primary-change controls are invented',await page.getByRole('button',{name:'Delete image',exact:true}).count()===0);
 await fill(page,{listPrice:'1234567.89'});
 const repriced=await mutation(page,`/api/units/${created.id}`,'PUT','Save changes');
 check('Advertisement edit leaves original external confirmation and publication intact',repriced.confirmedPrice==='9999999999999999.99'&&repriced.listPrice==='1234567.89'&&repriced.priceMatchesAdvertisement===false&&repriced.publicationStatus==='Published');
 await fill(page,{listPrice:'9999999999999999.99'});await mutation(page,`/api/units/${created.id}`,'PUT','Save changes');

 // A real concurrent write, then a stale UI write: the UI must not replay it.
 const current=await (await context.request.get(base+`/api/units/${created.id}`)).json();
 const body={title:'Unit after concurrent update '+prefix,description:current.description,unitType:current.unitType,areaSqm:current.areaSqm,bedrooms:current.bedrooms,bathrooms:current.bathrooms,listPrice:current.listPrice,currency:current.currency,version:current.version};
 check('Owned competing writer actually commits with the current shared Version',(await direct(context,`/api/units/${created.id}`,'PUT',body)).status()===200);
 await fill(page,{title:'Unsaved draft '+prefix});await mutation(page,`/api/units/${created.id}`,'PUT','Save changes',409);
 check('Stale conflict keeps the draft but disables save',await page.locator('#field-title').inputValue()==='Unsaved draft '+prefix&&await page.getByRole('button',{name:'Save changes',exact:true}).isDisabled());
 await page.getByRole('button',{name:'Reload data',exact:true}).click();await idle(page);
 check('Explicit reload adopts the actual server value',await page.locator('#field-title').inputValue()===body.title);
 let lost=0;let committed=false;
 await page.route(`**/api/units/${created.id}`,async route=>{if(route.request().method()!=='PUT')return route.continue();lost++;const response=await route.fetch();committed=response.status()===200;await route.abort('failed');});
 await fill(page,{title:'Unit with saved result '+prefix});await page.getByRole('button',{name:'Save changes',exact:true}).click();await page.getByRole('alert').filter({hasText:'Operation result is uncertain'}).waitFor();await idle(page);
 check('Lost post-commit response is unknown and not retried',committed&&lost===1&&await page.getByRole('button',{name:'Save changes',exact:true}).isDisabled());
 await page.unroute(`**/api/units/${created.id}`);await page.getByRole('button',{name:'Reload data',exact:true}).click();await idle(page);
 check('Reload discovers the actually saved unknown-outcome value',await page.locator('#field-title').inputValue()==='Unit with saved result '+prefix);
 await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:fileURLToPath(new URL('unit-admin-desktop.png',output)),fullPage:true});
 await page.setViewportSize({width:320,height:800});check('320px management layout has no horizontal overflow',await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:fileURLToPath(new URL('unit-admin-mobile-320.png',output)),fullPage:true});await page.setViewportSize({width:1440,height:1000});

 const future=await project(page,dev.id,prefix+'-NEW');const sameCode=await unit(page,future.id,'U1','New project unit '+prefix,'20.00');fixtures.dynamicUnitId=sameCode.id;
 await mutation(page,`/api/units/${sameCode.id}/publish`,'POST','Publish listing');
 check('AllDeveloperProjects covers a project created after agreement activation',true);
 await unit(page,proj.id,'U1','Duplicate code '+prefix,'1.00',409);
 check('Duplicate code in the same project is rejected while another project allowed it',await page.getByRole('alert').innerText().then(text=>text.includes('Conflict')));
 const selectedDev=await developer(page,prefix+'-SELECTED');const p1=await project(page,selectedDev.id,prefix+'-S1');const p2=await project(page,selectedDev.id,prefix+'-S2');
 const selectedAgreement=await agreement(page,selectedDev.id,prefix+'-SELECTED','SelectedProjects',[p1]);await mutation(page,`/api/marketing-agreements/${selectedAgreement.id}/activate`,'POST','Activate agreement');
 const excluded=await unit(page,p2.id,'U1','Out-of-scope unit '+prefix,'10.00');await mutation(page,`/api/units/${excluded.id}/publish`,'POST','Publish listing',409);
 check('SelectedProjects does not implicitly cover a sibling project',true);

 // Current parent eligibility hides public content without rewriting publication.
 await section(page,'developers',dev.id);await mutation(page,`/api/developers/${dev.id}/deactivate`,'POST','Deactivate record');
 check('Inactive parent immediately hides public detail and image',(await context.request.get(base+`/api/catalog/units/${created.id}`)).status()===404&&(await context.request.get(base+`/api/catalog/units/${created.id}/images/${fixtures.imageId}/content`)).status()===404);
 await mutation(page,`/api/developers/${dev.id}/reactivate`,'POST','Reactivate record');
 check('Reactivation restores current public eligibility without fabricated re-publication',(await context.request.get(base+`/api/catalog/units/${created.id}`)).status()===200);

 const visitor=await createContext();
 await visitor.page.goto(base+'/catalog?q='+prefix);
 await visitor.page.locator('.property-card h3 a').first().waitFor();await visitor.page.getByText('Search and sort filters',{exact:true}).click();
 await visitor.page.locator('#filter-minPrice').fill('9999999999999999.98');await visitor.page.locator('#filter-maxPrice').fill('9999999999999999.99');await visitor.page.locator('#filter-currency').fill('EGP');await visitor.page.locator('#filter-sort').selectOption('priceAsc');await visitor.page.locator('#filter-projectId').selectOption(proj.id);await visitor.page.locator('#filter-developerId').selectOption(dev.id);await visitor.page.locator('#filter-bedrooms').fill('2');await visitor.page.locator('#filter-minArea').fill('150.25');await visitor.page.locator('#filter-maxArea').fill('150.25');await visitor.page.locator('#filter-unitType').fill('Apartment');await visitor.page.locator('#filter-area').fill('Test area '+prefix);
 await visitor.page.getByRole('button',{name:'Search',exact:true}).click();await visitor.page.waitForURL(url=>url.searchParams.get('minPrice')==='9999999999999999.98');await visitor.page.locator('.property-card h3 a').first().waitFor();
 check('Live public combined filters persist exact decimal/GUID/UTF8 parameters and select the intended unit',await visitor.page.locator('.property-card h3 a').count()===1&&(await visitor.page.locator('.property-grid').innerText()).includes('Unit with saved result '+prefix));
 check('Maximum public money renders without rounding',(await visitor.page.locator('.property-grid').innerText()).includes(new Intl.NumberFormat('en-US',{minimumFractionDigits:0,maximumFractionDigits:2}).format('9999999999999999.99')));
 await visitor.page.evaluate(()=>window.scrollTo(0,0));await visitor.page.screenshot({path:fileURLToPath(new URL('public-filters-desktop.png',output)),fullPage:true});
 await visitor.page.locator('#filter-maxPrice').fill('9999999999999999.98');await visitor.page.getByRole('button',{name:'Search',exact:true}).click();await visitor.page.getByText('No properties match your search',{exact:true}).waitFor();check('A one-cent upper bound excludes the maximum-priced unit',true);
 const invalid=await visitor.page.goto(base+'/catalog?minPrice=1');await visitor.page.getByText('Review search filters',{exact:true}).waitFor();check('Invalid price-without-currency deep link has actual HTTP400',invalid.status()===400);
 const publicDto=await (await visitor.context.request.get(base+`/api/catalog/units/${created.id}`)).text();const html=await (await context.request.get(base+`/catalog/${created.id}`)).text();
 check('Public API and authenticated public SSR exclude contacts, internal notes and versions',!publicDto.includes('private-catalog-contact')&&!publicDto.includes('private-confirmation-note')&&!publicDto.includes('"version"')&&!html.includes('private-catalog-contact')&&!html.includes('private-confirmation-note')&&!html.includes(env.ADMIN_EMAIL));
 check('Updated public image URL streams actual normalized PNG',(await visitor.context.request.get(base+`/api/catalog/units/${created.id}/images/${fixtures.imageId}/content`)).status()===200);
 for(const [email,role] of [['browser@brokerhub.test','Customer'],['employee@brokerhub.test','SalesEmployee'],['manager@brokerhub.test','SalesManager']]){
  const denied=await createContext();await login(denied.page,email,env.SMOKE_PASSWORD);
  check(`${role} direct management GET is actually403`,(await denied.context.request.get(base+`/api/units/${created.id}`)).status()===403);
  await denied.page.goto(base+'/admin/developers');await denied.page.waitForURL('**/access?reason=forbidden');check(`${role} cannot render the management editor`,await denied.page.locator('#field-code').count()===0);await denied.context.close();
 }
 await section(page,'developers',dev.id);
 await page.route(`**/api/developers/${dev.id}`,route=>route.request().method()==='GET'?route.fulfill({status:403,contentType:'application/problem+json',body:'{}'}):route.continue());
 await page.getByRole('button',{name:'Reload data',exact:true}).click();
 await page.getByRole('alert').filter({hasText:'Administration session or permission'}).waitFor();
 check('Explicit global catalog403 clears previously authorized private editor and contacts',await page.locator('#field-externalContactEmail').count()===0&&!(await page.locator('app-catalog-admin-page').innerText()).includes('private-catalog-contact'));
 check('No uncaught application errors',errors.length===0);await visitor.context.close();await context.close();
}catch(error){
 if(debugPage && !debugPage.isClosed()){
  failureState=await debugPage.evaluate(()=>({alerts:[...document.querySelectorAll('[role="alert"]')].map(el=>el.textContent),invalid:[...document.querySelectorAll('input.ng-invalid,select.ng-invalid,textarea.ng-invalid')].map(el=>el.id),ruleValues:[...document.querySelectorAll('.commission-rule input,.commission-rule select')].map(el=>({id:el.id,value:el.value,disabled:el.disabled}))})).catch(()=>undefined);
  await debugPage.screenshot({path:fileURLToPath(new URL('failure.png',output)),fullPage:true}).catch(()=>undefined);
 }
 failure=safeFailure(error).replace(/https?:\/\/[^\s"'<>]+/g,value=>{try{const url=new URL(value);return url.origin+url.pathname+(url.search?'?[redacted]':'');}catch{return '[redacted-url]';}});for(const secret of [env.ADMIN_PASSWORD,env.SMOKE_PASSWORD])if(secret)failure=failure.split(secret).join('[redacted-secret]');}
finally{await browser.close();}
const report={status:failure?'Failed':'Passed',browser:version,checks:checks.length,error:failure,failureState,fixtures,results:checks,network,uncaughtErrors:errors};await writeFile(new URL('result.json',output),JSON.stringify(report,null,2));console.log(JSON.stringify({status:report.status,checks:checks.length,error:failure,failureState,fixtures},null,2));if(failure)process.exitCode=1;
