import { safeFailure } from './diagnostics.mjs';
import { chromium } from 'playwright-core';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const root = new URL('../../', import.meta.url);
const env = Object.fromEntries((await readFile(new URL('.env', root), 'utf8')).split(/\r?\n/).filter(line => line.includes('=')).map(line => [line.slice(0,line.indexOf('=')).replace(/^\uFEFF/,''),line.slice(line.indexOf('=')+1)]));
const base = `http://localhost:${env.WEB_PORT ?? 8080}`;
const output = new URL('.artifacts/browser-S02/', root);
await mkdir(output,{recursive:true});
const checks = []; const statuses = []; const errors = []; let failure; let fresh = 0;
const check = (label, condition) => { checks.push({label,passed:!!condition}); if(!condition) throw new Error(`Check failed: ${label}`); };
const browser = await chromium.launch({channel:'chrome',headless:true});
async function context() {
  const context = await browser.newContext({viewport:{width:1280,height:900},locale:'en-US'});
  await context.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
  const page = await context.newPage(); page.on('pageerror',error=>errors.push(error.message));
  page.on('response',response=>{const url=new URL(response.url()); if(url.pathname.startsWith('/api/')) statuses.push({path:url.pathname,method:response.request().method(),status:response.status()});});
  return {context,page};
}
async function login(page,email,password) {
  await page.goto(base+'/auth/login');
  await page.getByLabel('Email',{exact:true}).fill(email);
  await page.getByLabel('Password',{exact:true}).fill(password);
  const response = page.waitForResponse(response=>new URL(response.url()).pathname==='/api/accounts/login' && response.request().method()==='POST');
  await page.getByRole('button',{name:'Sign in',exact:true}).click();
  const status=(await response).status();
  if(status===200) { await page.waitForURL('**/auth/session'); await page.locator('.account-panel bdi').filter({hasText:email}).waitFor(); }
  return status;
}
async function createForm(page,email,role) {
  await page.getByLabel('Email',{exact:true}).fill(email);
  await page.getByLabel('Phone number',{exact:true}).fill('+201000000002');
  await page.getByLabel('Role',{exact:true}).selectOption(role);
  await page.getByLabel('Initial password',{exact:true}).fill(env.SMOKE_PASSWORD);
}
async function deniedDirect(context) {
  const pair=await (await context.request.get(base+'/api/session/antiforgery')).json();
  return (await context.request.post(base+'/api/staff-accounts',{headers:{Origin:base,'X-CSRF-TOKEN':pair.token},data:{email:'denied@brokerhub.test',password:env.SMOKE_PASSWORD,phoneNumber:'+201000000002',role:'SalesEmployee'}})).status();
}
try {
  const admin = await context();
  check('Actual Administrator login',await login(admin.page,env.ADMIN_EMAIL,env.ADMIN_PASSWORD)===200);
  await admin.page.goto(base+'/workspace');
  await admin.page.getByRole('link',{name:'Create staff account available',exact:true}).waitFor();
  check('Administrator workspace has provisioning and no implicit sales/customer navigation',await admin.page.getByRole('link',{name:'Deals and commissions coming soon',exact:true}).count()===0 && await admin.page.getByRole('link',{name:'My viewings coming soon',exact:true}).count()===0);
  await admin.page.getByRole('link',{name:'Create staff account available',exact:true}).click();
  await admin.page.getByLabel('Role',{exact:true}).waitFor();
  check('Only three approved staff roles exist in the form',JSON.stringify(await admin.page.locator('#staff-role option').evaluateAll(options=>options.map(option=>option.value)))===JSON.stringify(['SalesEmployee','SalesManager','SystemAdministrator']));
  // A dropped browser request proves honest unknown-outcome UX and no automatic replay.
  let dropped=0;
  await admin.page.route('**/api/staff-accounts',route=>{dropped++; return route.abort('failed');});
  await createForm(admin.page,'employee@brokerhub.test','SalesEmployee');
  await admin.page.getByRole('button',{name:'Create account',exact:true}).click();
  await admin.page.getByRole('alert').filter({hasText:'Account creation result is uncertain'}).waitFor();
  check('Unknown mutation outcome is explicit and never automatically retried',dropped===1 && await admin.page.locator('#staff-password').inputValue()==='');
  await admin.page.unroute('**/api/staff-accounts');

  for(const [email,role,expected,denied] of [
    ['employee@brokerhub.test','SalesEmployee','Assigned customers available',true],
    ['manager@brokerhub.test','SalesManager','Customer assignment available',true],
    ['staff-admin@brokerhub.test','SystemAdministrator','Create staff account available',false],
  ]) {
    await createForm(admin.page,email,role);
    const response=admin.page.waitForResponse(response=>new URL(response.url()).pathname==='/api/staff-accounts' && response.request().method()==='POST');
    await admin.page.getByRole('button',{name:'Create account',exact:true}).click();
    const status=(await response).status();
    check(`Administrator actual ${role} creation201 or explicit existing fixture400`,[201,400].includes(status));
    if(status===201){fresh++; await admin.page.getByRole('status').filter({hasText:'Account created'}).waitFor();}
    else await admin.page.getByRole('alert').filter({hasText:'The server rejected the data'}).waitFor();
    check(`${role} password is cleared after outcome`,await admin.page.locator('#staff-password').inputValue()==='');
    const staff=await context();
    let statusLogin=await login(staff.page,email,env.SMOKE_PASSWORD);
    if(statusLogin===403){
      await staff.page.getByRole('button',{name:'Prepare confirmation link again'}).click();
      await staff.page.getByRole('status').filter({hasText:'A new confirmation link was prepared'}).waitFor();
      const link=execFileSync('pwsh',['-NoProfile','-File',fileURLToPath(new URL('scripts/Get-LocalIdentityLink.ps1',root)),'-Recipient',email],{cwd:fileURLToPath(root),encoding:'utf8',stdio:['ignore','pipe','pipe']}).trim();
      const url=new URL(link); if(url.origin!==base || url.pathname!=='/auth/confirm-email')throw new Error('Unexpected local confirmation path');
      await staff.page.goto(link);
      await staff.page.getByRole('button',{name:'Confirm email',exact:true}).click();
      await staff.page.getByRole('status').filter({hasText:'Email confirmed'}).waitFor();
      statusLogin=await login(staff.page,email,env.SMOKE_PASSWORD);
    }
    check(`${role} confirmed account logs in using actual cookie API`,statusLogin===200);
    await staff.page.goto(base+'/workspace');
    await staff.page.getByRole('link',{name:expected,exact:true}).waitFor();
    check(`${role} gets its actual workspace destination`,true);
    if(denied){
      check(`${role} direct staff-account API is forbidden`,await deniedDirect(staff.context)===403);
      await staff.page.goto(base+'/admin/staff-accounts'); await staff.page.waitForURL('**/access?reason=forbidden');
      check(`${role} direct admin route does not expose the form`,await staff.page.locator('#staff-email').count()===0);
    }
    await staff.context.close();
  }
  check('At least one real201 is recorded, or rerun explicitly uses previously created owned fixtures',fresh>0 || statuses.filter(item=>item.path==='/api/staff-accounts'&&item.status===400).length===3);
  await admin.page.locator('#staff-email').focus();
  await admin.page.keyboard.press('Tab');
  const phoneFocused=await admin.page.evaluate(()=>document.activeElement?.id==='staff-phone');
  await admin.page.keyboard.press('Tab');
  const roleFocused=await admin.page.evaluate(()=>document.activeElement?.id==='staff-role');
  await admin.page.keyboard.press('Tab');
  check('Provisioning fields are reachable in keyboard order email/phone/role/password',phoneFocused && roleFocused && await admin.page.evaluate(()=>document.activeElement?.id==='staff-password'));
  // Full-page Chrome capture can paint fixed offscreen content at the current scroll offset.
  // Start at the document top so the hidden keyboard skip link is clipped correctly.
  await admin.page.evaluate(()=>window.scrollTo(0,0));
  await admin.page.screenshot({path:fileURLToPath(new URL('staff-desktop.png',output)),fullPage:true});
  await admin.page.setViewportSize({width:320,height:800});
  check('320px provisioning form has no horizontal overflow',await admin.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await admin.page.evaluate(()=>window.scrollTo(0,0));
  await admin.page.screenshot({path:fileURLToPath(new URL('staff-mobile-320.png',output)),fullPage:true});

  const customer=await context();
  check('Customer existing account login',await login(customer.page,'browser@brokerhub.test',env.SMOKE_PASSWORD)===200);
  check('Customer direct staff-account API is forbidden',await deniedDirect(customer.context)===403);
  await customer.page.goto(base+'/admin/staff-accounts'); await customer.page.waitForURL('**/access?reason=forbidden');
  check('Customer direct route403 shows no staff fields',await customer.page.locator('#staff-email').count()===0);
  await customer.context.clearCookies();
  await customer.page.goto(base+'/my/bookings'); await customer.page.waitForURL('**/auth/login?returnUrl=*');
  check('Expired cookie redirects to login with the exact safe destination',new URL(customer.page.url()).searchParams.get('returnUrl')==='/my/bookings');
  await customer.page.route('**/api/accounts/me',route=>route.fulfill({status:503,contentType:'application/problem+json',body:'{}'}));
  await customer.page.goto(base+'/workspace'); await customer.page.waitForURL('**/access?reason=unavailable&returnUrl=*');
  check('Session outage is explicit unavailable, not fake guest or workspace',await customer.page.getByRole('alert').innerText().then(text=>text.includes('Service unavailable')));
  await customer.context.close();

  // Hold an actual old Administrator /me response while a peer tab changes the shared cookie.
  await admin.page.setViewportSize({width:1280,height:900});
  let release; const gate=new Promise(resolve=>{release=resolve;});
  let observed; const reached=new Promise(resolve=>{observed=resolve;}); let hold=true;
  await admin.page.route('**/api/accounts/me',async route=>{
    if(!hold) return route.continue(); hold=false;
    const response=await route.fetch(); observed(); await gate;
    try {await route.fulfill({response});}catch{/* Expected only when the old browser request was canceled. */}
  });
  await admin.page.goto(base+'/auth/session'); await reached;
  const peer=await admin.context.newPage();
  check('Peer tab changes the actual shared cookie to Customer',await login(peer,'browser@brokerhub.test',env.SMOKE_PASSWORD)===200);
  release();
  await admin.page.getByRole('status').filter({hasText:'Contacting server'}).waitFor({state:'hidden'});
  check('Late old-session response cannot restore Administrator identity after peer login',!(await admin.page.locator('.account-panel').innerText()).includes(env.ADMIN_EMAIL));
  await admin.page.goto(base+'/admin/staff-accounts'); await admin.page.waitForURL('**/access?reason=forbidden');
  check('Peer identity change revokes the Administrator form and cached result',await admin.page.locator('#staff-email').count()===0);
  check('Session/CSRF data is never persisted in browser storage',await admin.page.evaluate(()=>localStorage.length===0 && sessionStorage.length===0));
  check('No uncaught browser application errors',errors.length===0);
  await admin.context.close();
} catch(error) {
  failure=safeFailure(error).replace(/https?:\/\/[^\s"'<>]+/g,value=>{try{const url=new URL(value);return url.origin+url.pathname+(url.search?'?[redacted]':'');}catch{return '[redacted-url]';}});
  for(const secret of [env.ADMIN_PASSWORD,env.SMOKE_PASSWORD])if(secret)failure=failure.split(secret).join('[redacted-secret]');
} finally {await browser.close();}
const report={status:failure?'Failed':'Passed',checks:checks.length,freshStaffAccounts:fresh,error:failure,results:checks,statuses,uncaughtErrors:errors};
await writeFile(new URL('result.json',output),JSON.stringify(report,null,2));
console.log(JSON.stringify({status:report.status,checks:checks.length,freshStaffAccounts:fresh,error:failure},null,2));
if(failure)process.exitCode=1;
