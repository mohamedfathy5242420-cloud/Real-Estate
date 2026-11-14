import { safeFailure } from './diagnostics.mjs';
import { chromium } from 'playwright-core';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const root = new URL('../../', import.meta.url);
const env = Object.fromEntries((await readFile(new URL('.env', root), 'utf8')).split(/\r?\n/).filter(line => line.includes('=')).map(line => [line.slice(0,line.indexOf('=')).replace(/^\uFEFF/,''), line.slice(line.indexOf('=')+1)]));
const base = process.env.BASE_URL ?? `http://localhost:${env.WEB_PORT ?? 8080}`;
const email = 'browser@brokerhub.test';
const password = env.SMOKE_PASSWORD;
if (!password) throw new Error('Run the local smoke fixture before browser checks.');
const output = new URL('.artifacts/browser/', root);
await mkdir(output, { recursive: true });
const checks = [];
const network = [];
const pageErrors = [];
function check(label, condition) { checks.push({label,passed:Boolean(condition)}); if(!condition) throw new Error(`Check failed: ${label}`); }
const browser = await chromium.launch({ channel: process.env.BROWSER_CHANNEL ?? 'chrome', headless: true });
let failure;
try {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'en-US' });
  await context.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
  const page = await context.newPage();
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('response', response => network.push({ path: new URL(response.url()).pathname, method: response.request().method(), status: response.status() }));
  const publicPage = await context.request.get(base + '/api/catalog/units?q=' + encodeURIComponent('Integration test'));
  const fixture = (await publicPage.json()).items[0];
  check('Real catalog fixture is available before UI checks', publicPage.status() === 200 && fixture?.id);
  const currentPage = await context.request.get(base + '/api/catalog/units');
  const latest = (await currentPage.json()).items[0];
  if(currentPage.status() !== 200 || !latest?.id) throw new Error('Current default catalog page unavailable');

  await page.goto(base + '/catalog');
  await page.locator('.property-card h3 a').first().waitFor();
  check('English LTR browser document', await page.locator('html').getAttribute('lang') === 'en' && await page.locator('html').getAttribute('dir') === 'ltr');
  check('Hydrated browser cards show actual API title and precise price', (await page.locator('.property-card h3 a').first().getAttribute('href')) === '/catalog/' + latest.id && (await page.locator('.property-grid').innerText()).includes(latest.title) && (await page.locator('.property-grid').innerText()).includes(new Intl.NumberFormat('en-US', {maximumFractionDigits:2}).format(latest.listPrice)));
  check('Desktop has no horizontal overflow', await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await page.screenshot({ path: fileURLToPath(new URL('catalog-desktop.png', output)), fullPage: true });
  await page.getByRole('searchbox', { name: 'Area, project or keyword' }).fill('Integration test');
  await page.getByRole('button', { name: 'Search', exact: true }).click();
  await page.waitForURL(url => url.searchParams.get('q') === 'Integration test');
  await page.locator('.property-card h3 a').first().waitFor();
  check('Browser search persists in URL and loads live results', (await page.locator('.property-grid').innerText()).includes(fixture.title) && (await page.locator('.property-grid').innerText()).includes('1,234,567.89'));
  await page.locator('.property-card h3 a').first().click();
  await page.waitForURL('**/catalog/' + fixture.id);
  await page.locator('.unit-description').waitFor();
  check('Detail browser route loads real API description', (await page.locator('.unit-description').innerText()).includes('Not a real property offer'));
  check('Published image keeps its actual alternative text', await page.locator('.unit-art img').getAttribute('alt') === fixture.previewImage.altText);
  check('Published image bytes loaded in browser', await page.locator('.unit-art img').evaluate(image => image.complete && image.naturalWidth > 0));
  await page.screenshot({ path: fileURLToPath(new URL('detail-desktop.png', output)), fullPage: true });

  await page.goto(base + '/auth/login');
  await page.getByLabel('Email', { exact: true }).waitFor();
  for(let step=0; step<12 && await page.evaluate(() => document.activeElement?.id) !== 'account-email'; step++) await page.keyboard.press('Tab');
  check('Email input is keyboard reachable', await page.evaluate(() => document.activeElement?.id) === 'account-email');
  await page.keyboard.press('Tab');
  check('Password follows email in keyboard order', await page.evaluate(() => document.activeElement?.id) === 'account-password');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign in', exact: true }).focus();
  await page.keyboard.press('Enter');
  await page.waitForURL('**/auth/session');
  await page.getByText(email, { exact: true }).waitFor();
  check('Keyboard login reaches actual current-session UI', (await page.locator('.account-panel').innerText()).includes(email));
  const cookies = await context.cookies();
  const authentication = cookies.find(cookie => cookie.name === 'BrokerHub.Auth.Development');
  check('Browser uses an HttpOnly SameSite Lax development session cookie', authentication?.httpOnly === true && authentication.sameSite === 'Lax');
  check('No auth/CSRF token is persisted to localStorage or sessionStorage', await page.evaluate(() => localStorage.length === 0 && sessionStorage.length === 0));
  const loginIndex = network.findIndex(item => item.path === '/api/accounts/login' && item.method === 'POST' && item.status === 200);
  check('Browser bootstraps CSRF before login and refreshes it after login', network.slice(0,loginIndex).some(item => item.path === '/api/session/antiforgery') && network.slice(loginIndex+1).some(item => item.path === '/api/session/antiforgery'));
  const originalImage = await (await context.request.get(base + fixture.previewImage.contentUrl)).body();
  execFileSync('docker', ['compose', 'restart', 'api'], { cwd:fileURLToPath(root), stdio:'pipe' });
  let ready = false;
  for(let attempt=0;attempt<30;attempt++) {
    try { if((await context.request.get(base + '/readyz', {timeout:5000})).status() === 200) { ready=true;break; } } catch { /* bounded GET-only recovery polling */ }
    await new Promise(resolve => setTimeout(resolve,500));
  }
  check('Own API container recovers after a controlled restart', ready);
  const restoredSession = await context.request.get(base + '/api/accounts/me');
  check('HttpOnly session survives API restart with persisted Data Protection keys', restoredSession.status() === 200 && (await restoredSession.json()).email === email);
  const restoredImage = await context.request.get(base + fixture.previewImage.contentUrl);
  check('Published image bytes survive API restart in the private volume', restoredImage.status() === 200 && originalImage.equals(await restoredImage.body()));
  await page.goto(base + '/auth/recovery');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByRole('button', { name: 'Reset password', exact: true }).click();
  await page.locator('.message.success').waitFor();
  check('Recovery form displays generic real-API acceptance', (await page.locator('.message.success').innerText()).includes('If the account is eligible'));
  const ownResetLink = execFileSync('pwsh', ['-NoProfile','-File',fileURLToPath(new URL('scripts/Get-LocalIdentityLink.ps1',root)),'-Recipient',email,'-Kind','password-reset'], {cwd:fileURLToPath(root),encoding:'utf8',stdio:['ignore','pipe','pipe']}).trim();
  check('Owned reset capture targets this local application', new URL(ownResetLink).origin === base && new URL(ownResetLink).pathname === '/auth/reset-password');
  await page.goto(ownResetLink);
  await page.getByLabel('New password', {exact:true}).fill(password);
  await page.getByRole('button', {name:'Set a new password',exact:true}).click();
  await page.locator('.message.success').waitFor();
  await page.waitForURL(url => url.pathname === '/auth/reset-password' && url.search === '');
  check('Reset UI succeeds and removes the secret query from browser history', (await page.locator('.message.success').innerText()).includes('Password changed'));
  check('Reset invalidates the previous authenticated cookie', (await context.request.get(base + '/api/accounts/me')).status() === 401);
  await page.locator('.account-panel nav').getByRole('link',{name:'Sign in',exact:true}).click();
  await page.waitForURL('**/auth/login');
  await page.getByLabel('Email', {exact:true}).fill(email);
  await page.getByLabel('Password', {exact:true}).fill(password);
  await page.getByRole('button', {name:'Sign in',exact:true}).click();
  await page.waitForURL('**/auth/session');
  await page.getByText(email,{exact:true}).waitFor();
  check('First login after reset uses a renewed anonymous CSRF pair', network.filter(item => item.path === '/api/accounts/login' && item.method === 'POST').every(item => item.status === 200));
  await page.goto(base + '/auth/session');
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  await page.waitForURL('**/auth/login');
  check('Browser logout invalidates the actual API session', (await context.request.get(base + '/api/accounts/me')).status() === 401);

  let failNext = true;
  await page.route('**/api/catalog/units?**', route => {
    if(failNext) { failNext = false; return route.fulfill({status:503,contentType:'application/problem+json',body:'{"status":503}'}); }
    return route.continue();
  });
  await page.goto(base + '/catalog?q=' + encodeURIComponent('Integration test'));
  await page.getByText('Could not load properties', {exact:true}).waitFor();
  check('Injected browser503 shows an error without stale or Demo cards', await page.locator('.property-card').count() === 0);
  await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await page.locator('.property-card h3 a').first().waitFor();
  check('Retry recovers with actual API data', (await page.locator('.property-grid').innerText()).includes(fixture.title));

  const mobile = await browser.newContext({ viewport: {width:320,height:800}, isMobile:true, hasTouch:true, locale:'en-US' });
  await mobile.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
  const phone = await mobile.newPage();
  phone.on('pageerror', error => pageErrors.push(error.message));
  await phone.goto(base + '/catalog');
  await phone.locator('.property-card h3 a').first().waitFor();
  check('320px catalog has no horizontal overflow', await phone.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await phone.getByRole('button', {name:'Toggle navigation menu'}).click();
  await phone.waitForFunction(() => document.querySelector('.nav-toggle')?.getAttribute('aria-expanded') === 'true');
  check('Mobile navigation expands accessibly', await phone.getByRole('button', {name:'Toggle navigation menu'}).getAttribute('aria-expanded') === 'true');
  await phone.screenshot({path:fileURLToPath(new URL('catalog-mobile-320.png',output)),fullPage:true});
  await phone.goto(base + '/catalog/' + fixture.id);
  await phone.locator('.unit-description').waitFor();
  check('320px detail has no horizontal overflow', await phone.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await phone.screenshot({path:fileURLToPath(new URL('detail-mobile-320.png',output)),fullPage:true});
  await phone.goto(base + '/auth/login');
  await phone.getByLabel('Email', {exact:true}).waitFor();
  check('320px account form has no horizontal overflow', await phone.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  await phone.screenshot({path:fileURLToPath(new URL('login-mobile-320.png',output)),fullPage:true});
  check('No uncaught browser application error', pageErrors.length === 0);
  await mobile.close(); await context.close();
} catch(error) {
  failure = safeFailure(error).replace(/https?:\/\/[^\s"'<>]+/g, value => {
    try { const url = new URL(value); return url.origin + url.pathname + (url.search ? '?[redacted]' : ''); }
    catch { return '[redacted-url]'; }
  }).split(password).join('[redacted-secret]');
}
finally { await browser.close(); }
const report = { status: failure ? 'Failed' : 'Passed', browser:'system Chrome headless / Playwright-core1.64.1', checks:checks.length, error:failure, results:checks, network, uncaughtErrors:pageErrors };
await writeFile(new URL('result.json',output), JSON.stringify(report,null,2));
console.log(JSON.stringify({status:report.status,checks:checks.length,error:failure,output:fileURLToPath(output)},null,2));
if(failure) process.exitCode=1;
