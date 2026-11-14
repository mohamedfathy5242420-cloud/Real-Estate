// I01-S09 all-role delivered route acceptance. Read-only navigation, no fabricated API replies.
import {chromium} from 'playwright-core';import {readFile,writeFile,mkdir} from 'node:fs/promises';import {fileURLToPath} from 'node:url';import {safeFailure} from './diagnostics.mjs';
const root=new URL('../../',import.meta.url),output=new URL('.artifacts/browser-S09-navigation/',root);await mkdir(output,{recursive:true});
const env=Object.fromEntries((await readFile(new URL('.env',root),'utf8')).split(/\r?\n/).filter(x=>x.includes('=')).map(x=>[x.slice(0,x.indexOf('=')).replace(/^\uFEFF/,''),x.slice(x.indexOf('=')+1)]));
const base=`http://localhost:${env.WEB_PORT??8080}`,checks=[],network=[],errors=[];let failure,debugPage;
const check=(label,value)=>{checks.push({label,passed:!!value});if(!value)throw Error(label);console.log('Passed: '+label);};
const roles=[
 {role:'Customer',email:'browser@brokerhub.test',paths:['/my/profile','/my/viewings','/my/bookings','/notifications']},
 {role:'SalesEmployee',email:'employee@brokerhub.test',paths:['/employee/clients','/employee/viewings','/employee/bookings','/employee/reports','/notifications']},
 {role:'SalesManager',email:'manager@brokerhub.test',paths:['/admin/clients','/admin/customer-links','/employee/viewings','/admin/bookings','/admin/deals','/admin/reports','/notifications']},
 {role:'SystemAdministrator',email:env.ADMIN_EMAIL,paths:['/admin/staff-accounts','/admin/employees','/admin/developers']},
];
const browser=await chromium.launch({channel:'chrome',headless:true});
async function idle(page){await page.waitForFunction(()=>!document.querySelector('[aria-busy="true"]'));await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r))));}
try{
 for(const config of roles){
  const context=await browser.newContext({viewport:{width:1280,height:1000},locale:'en-US',timezoneId:'Africa/Cairo'});await context.route('**/*',r=>new URL(r.request().url()).origin===base?r.continue():r.abort());const page=await context.newPage();debugPage=page;page.setDefaultTimeout(20000);page.on('pageerror',e=>errors.push(safeFailure(e)));page.on('response',r=>{const u=new URL(r.url());if(u.pathname.startsWith('/api/'))network.push({role:config.role,path:u.pathname,method:r.request().method(),status:r.status()});});
  await page.goto(base+'/auth/login');await page.locator('#account-email').fill(config.email);await page.locator('#account-password').fill(config.role==='SystemAdministrator'?env.ADMIN_PASSWORD:env.SMOKE_PASSWORD);await page.getByRole('button',{name:'Sign in',exact:true}).click();await page.waitForURL('**/auth/session');await page.getByText(config.email,{exact:true}).waitFor();
  const me=await(await context.request.get(base+'/api/accounts/me')).json();check(config.role+' actual current account has expected role',me.roles.includes(config.role));
  await page.goto(base+'/workspace');await page.getByRole('navigation',{name:'My account tasks',exact:true}).waitFor();const actual=await page.getByRole('navigation',{name:'My account tasks',exact:true}).locator('a').evaluateAll(a=>a.map(x=>x.getAttribute('href')).sort());check(config.role+' workspace exposes exactly approved delivered destinations',JSON.stringify(actual)===JSON.stringify([...config.paths].sort())&&!(await page.locator('app-workspace-page').textContent()).includes('Coming soon'));
  for(const path of config.paths){
   await page.goto(base+path);await page.locator('#route-heading').waitFor();await idle(page);const content=await page.locator('main').textContent();check(config.role+' opens real screen '+path,!page.url().includes('/access')&&!page.url().includes('/auth/login')&&!content.includes('This page is being prepared')&&!content.includes('Actions are not implemented yet')&&!content.includes('Page not found'));
   await page.setViewportSize({width:320,height:900});check(config.role+' usable320px '+path,await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.screenshot({path:fileURLToPath(new URL(config.role+'-'+path.slice(1).replaceAll('/','-')+'.png',output)),fullPage:false});await page.setViewportSize({width:1280,height:1000});
  }
  check(config.role+' private browser storage remains empty',await page.evaluate(()=>localStorage.length===0&&sessionStorage.length===0));await context.close();
 }
 check('All role navigation has no uncaught browser exception',errors.length===0);
}catch(e){failure=safeFailure(e,[env.ADMIN_PASSWORD,env.SMOKE_PASSWORD,env.RABBIT_PASSWORD]);if(debugPage)await debugPage.screenshot({path:fileURLToPath(new URL('failure.png',output)),fullPage:true}).catch(()=>{});}finally{await browser.close();}
const report={status:failure?'Failed':'Passed',checks:checks.length,error:failure,results:checks,network,uncaughtErrors:errors};await writeFile(new URL('result.json',output),JSON.stringify(report,null,2));console.log(JSON.stringify({status:report.status,checks:checks.length,error:failure},null,2));if(failure)process.exitCode=1;
