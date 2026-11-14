import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { safeFailure } from './diagnostics.mjs';

const { chromium } = await import(process.env.PLAYWRIGHT_MODULE ?? 'playwright-core');
const root = new URL('../../', import.meta.url);
const settings = Object.fromEntries(
  (await readFile(new URL('.env', root), 'utf8')).split(/\r?\n/)
    .filter(line => line.includes('='))
    .map(line => { const i = line.indexOf('='); return [line.slice(0, i).replace(/^\uFEFF/, ''), line.slice(i + 1)]; }),
);
const base = `http://localhost:${settings.WEB_PORT ?? 8080}`;
if (!settings.COMPOSE_PROJECT_NAME?.endsWith('showcase')) {
  throw new Error('Use a separate COMPOSE_PROJECT_NAME ending in showcase. This script creates fictional demonstration records.');
}
const output = new URL('assets/screenshots/', root);
const evidence = new URL('.artifacts/showcase/', root);
await mkdir(output, { recursive: true });
await mkdir(evidence, { recursive: true });
const checks = [];
const errors = [];
const serverErrors = [];
function check(label, condition) {
  checks.push({ label, passed: Boolean(condition) });
  if (!condition) throw new Error(label);
}
const browser = await chromium.launch({ channel: process.env.BROWSER_CHANNEL ?? 'chrome', headless: true });
async function session() {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, locale: 'en-US' });
  await context.route('**/*', route => new URL(route.request().url()).origin === base ? route.continue() : route.abort());
  const page = await context.newPage();
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    const url = new URL(response.url());
    if (url.origin === base && response.status() >= 500) serverErrors.push(url.pathname);
  });
  return { context, page, token: null };
}
async function csrf(s) {
  const response = await s.context.request.get(base + '/api/session/antiforgery');
  check('CSRF bootstrap succeeds', response.status() === 200);
  s.token = (await response.json()).token;
}
async function get(s, path) {
  const response = await s.context.request.get(base + path);
  check(`Read ${path.split('?')[0]}`, response.status() === 200);
  return response.json();
}
async function post(s, path, data, expected = 200, idempotent = false) {
  if (!s.token) await csrf(s);
  const response = await s.context.request.post(base + path, {
    headers: { Origin: base, 'X-CSRF-TOKEN': s.token, ...(idempotent ? { 'Idempotency-Key': randomUUID() } : {}) },
    data,
  });
  check(`Write ${path} returns ${expected}`, response.status() === expected);
  return expected === 204 ? null : response.json();
}
async function login(s, email, password) {
  await csrf(s);
  await post(s, '/api/accounts/login', { email, password, rememberMe: false });
  await csrf(s);
  return get(s, '/api/accounts/me');
}
async function visit(s, path) {
  await s.page.goto(base + path);
  await s.page.locator('#route-heading').waitFor();
  await s.page.waitForFunction(() => !document.querySelector('[aria-busy="true"]'));
  await s.page.evaluate(() => document.fonts.ready);
  check(`Expected route opens on ${path}`, new URL(s.page.url()).pathname === path);
  check(`English LTR document on ${path}`, await s.page.locator('html').getAttribute('lang') === 'en' && await s.page.locator('html').getAttribute('dir') === 'ltr');
  check(`English visible content on ${path}`, !/[\u0600-\u06ff]/.test(await s.page.locator('body').innerText()));
  check(`No horizontal overflow on ${path}`, await s.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
  check(`Private browser storage stays empty on ${path}`, await s.page.evaluate(() => localStorage.length === 0 && sessionStorage.length === 0));
}
async function screenshot(s, name, fullPage = false) {
  await s.page.screenshot({ path: fileURLToPath(new URL(name + '.png', output)), fullPage });
}
async function provision(admin, email, role) {
  const created = await admin.context.request.post(base + '/api/staff-accounts', {
    headers: { Origin: base, 'X-CSRF-TOKEN': admin.token },
    data: { email, password: settings.SMOKE_PASSWORD, phoneNumber: '+201000000002', role },
  });
  check(`Provision ${role}`, [201, 400].includes(created.status()));
  if (created.status() === 201) {
    const link = new URL(execFileSync('pwsh', ['-NoProfile', '-File', fileURLToPath(new URL('scripts/Get-LocalIdentityLink.ps1', root)), '-Recipient', email], { cwd: fileURLToPath(root), encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim());
    const anonymous = await session();
    await post(anonymous, '/api/accounts/confirm-email', { accountId: link.searchParams.get('accountId'), token: link.searchParams.get('token') }, 204);
    await anonymous.context.close();
  }
  const s = await session();
  const account = await login(s, email, settings.SMOKE_PASSWORD);
  const profiles = await get(admin, '/api/employees');
  if (!profiles.items.some(profile => profile.accountId === account.accountId)) {
    await post(admin, '/api/employees', { accountId: account.accountId, employeeNumber: role === 'SalesManager' ? 'DEMO-M01' : 'DEMO-E01', displayName: role === 'SalesManager' ? 'Demo Sales Manager' : 'Demo Property Advisor' }, 201);
  }
  return s;
}

let failure;
try {
  const anonymous = await session();
  const admin = await session();
  await login(admin, settings.ADMIN_EMAIL, settings.ADMIN_PASSWORD);
  const manager = await provision(admin, 'manager@brokerhub.test', 'SalesManager');
  const employee = await provision(admin, 'employee@brokerhub.test', 'SalesEmployee');
  const customer = await session();
  await login(customer, settings.SMOKE_EMAIL ?? 'smoke@brokerhub.test', settings.SMOKE_PASSWORD);
  const customerProfile = await customer.context.request.get(base + '/api/customers/me');
  if (customerProfile.status() === 404) {
    await post(customer, '/api/customers', { fullName: 'Demo Customer Account', phone: '+201000000100', email: 'smoke@brokerhub.test', preferredContactMethod: 'Email', requirements: 'A three-bedroom home with garden views.' }, 201);
  } else {
    check('Read existing customer profile', customerProfile.status() === 200);
  }
  const developers = await get(admin, '/api/developers');
  const integrationDeveloper = developers.find(item => item.code === 'DOCKER-INTEGRATION');
  if (integrationDeveloper) {
    const testProjects = await get(admin, `/api/projects?developerId=${integrationDeveloper.id}`);
    for (const testProject of testProjects.filter(item => item.code === 'DOCKER-INTEGRATION')) {
      const testUnits = await get(admin, `/api/units?projectId=${testProject.id}`);
      for (const unit of testUnits.filter(item => item.code === 'DOCKER-INTEGRATION' && item.publicationStatus === 'Published')) {
        await post(admin, `/api/units/${unit.id}/unpublish`, { version: unit.version, reason: 'Keep the integration fixture out of demonstration screenshots' });
      }
    }
  }
  let developer = developers.find(item => item.code === 'SHOWCASE');
  if (!developer) developer = await post(admin, '/api/developers', { code: 'SHOWCASE', name: 'Horizon Developments', legalName: 'Fictional demonstration developer', externalContactName: 'Demo Contact', externalContactEmail: 'contact@brokerhub.test', externalContactPhone: '+201000000010' }, 201);
  const projects = await get(admin, `/api/projects?developerId=${developer.id}`);
  let project = projects.find(item => item.code === 'CEDAR-GARDENS');
  if (!project) project = await post(admin, '/api/projects', { developerId: developer.id, code: 'CEDAR-GARDENS', name: 'Cedar Gardens', area: 'New Cairo', address: 'Fictional demonstration address', timeZoneId: 'Africa/Cairo' }, 201);
  const agreements = await get(admin, `/api/marketing-agreements?developerId=${developer.id}`);
  let agreement = agreements.find(item => item.agreementNumber === 'SHOWCASE-01');
  if (!agreement) agreement = await post(admin, '/api/marketing-agreements', { developerId: developer.id, agreementNumber: 'SHOWCASE-01', scopeType: 'AllDeveloperProjects', startsOn: '2020-01-01', endsOn: '2099-12-31', protectionPeriodDays: 90, projectIds: [], commissionRules: [{ projectId: null, ruleType: 'Percentage', percentage: '2.00', fixedAmount: null, currency: 'EGP' }] }, 201);
  if (agreement.status === 'Draft') await post(admin, `/api/marketing-agreements/${agreement.id}/activate`, { version: agreement.version });
  const existing = await get(admin, `/api/units?projectId=${project.id}`);
  const samples = [
    ['GARDEN-01', 'Garden Terrace Apartment', 'A light-filled three-bedroom apartment with generous living spaces and garden views.', '165.00', 3, 2, '6400000.00', 'garden'],
    ['CITY-02', 'The Courtyard Residence', 'An inviting family apartment with a practical layout overlooking a landscaped courtyard.', '142.00', 2, 2, '4850000.00', 'city'],
    ['COAST-03', 'Blue Shore Retreat', 'A relaxed two-bedroom retreat with open living spaces and a coastal-inspired design.', '120.00', 2, 1, '5200000.00', 'coast'],
    ['FAMILY-04', 'Cedar Family Residence', 'A spacious four-bedroom home designed for comfortable everyday family living.', '240.00', 4, 3, '9200000.00', 'garden'],
  ];
  const unitIds = [];
  for (const [code, title, description, areaSqm, bedrooms, bathrooms, listPrice, scene] of samples) {
    let unit = existing.find(item => item.code === code);
    if (!unit) unit = await post(admin, '/api/units', { projectId: project.id, code, title, description: description + ' Fictional demonstration listing; not a real property offer.', unitType: 'Apartment', areaSqm, bedrooms, bathrooms, listPrice, currency: 'EGP' }, 201);
    const images = await get(admin, `/api/units/${unit.id}/images`);
    if (!images.images.length) {
      const art = await browser.newPage({ viewport: { width: 1200, height: 800 } });
      await art.setContent(`<style>body{margin:0}img{display:block;width:1200px;height:800px;object-fit:cover}</style><img src="${base}/illustrations/${scene}.svg" alt="">`);
      await art.locator('img').evaluate(image => image.decode());
      const buffer = await art.screenshot();
      await art.close();
      const response = await admin.context.request.post(base + `/api/units/${unit.id}/images`, {
        headers: { Origin: base, 'X-CSRF-TOKEN': admin.token },
        multipart: { File: { name: 'illustration.png', mimeType: 'image/png', buffer }, AltText: 'Architectural illustration of a fictional home; not a property photograph', SortOrder: '0', Version: String(unit.version) },
      });
      check('Upload original application illustration', response.status() === 201);
      unit = await get(admin, `/api/units/${unit.id}`);
    }
    if (unit.publicationStatus !== 'Published') await post(admin, `/api/units/${unit.id}/publish`, { version: unit.version, reason: 'Fictional demonstration listing' });
    unitIds.push(unit.id);
  }
  const customers = await get(manager, '/api/crm/customers');
  for (const [fullName, phone, requirements] of [
    ['Demo Customer One', '+201000000101', 'Three bedrooms in New Cairo, with garden views.'],
    ['Demo Customer Two', '+201000000102', 'A family home with four bedrooms and flexible living space.'],
    ['Demo Customer Three', '+201000000103', 'A two-bedroom apartment with a practical layout.'],
  ]) {
    if (!customers.items.some(item => item.fullName === fullName)) await post(manager, '/api/crm/customers', { fullName, phone, email: null, preferredContactMethod: 'Phone', requirements }, 201, true);
  }

  await visit(anonymous, '/catalog');
  await anonymous.page.locator('.property-card h3 a').first().waitFor();
  await anonymous.page.locator('.property-art img').first().evaluate(image => image.decode());
  await screenshot(anonymous, 'catalog', true);
  await visit(anonymous, '/catalog/' + unitIds[0]);
  await anonymous.page.locator('.unit-description').waitFor();
  await screenshot(anonymous, 'property-details');
  await visit(manager, '/workspace');
  await manager.page.locator('app-workspace-page a[href="/admin/clients"]').waitFor();
  await screenshot(manager, 'workspace');
  await visit(manager, '/admin/clients');
  await manager.page.getByRole('button', { name: /Demo Customer One/ }).waitFor();
  await manager.page.getByRole('button', { name: /Demo Customer One/ }).click();
  await manager.page.getByRole('region', { name: 'Current customer profile', exact: true }).waitFor();
  await screenshot(manager, 'customers');
  await visit(admin, '/admin/developers');
  await admin.page.getByRole('button', { name: /Horizon Developments/ }).waitFor();
  await screenshot(admin, 'catalog-admin');
  await anonymous.page.setViewportSize({ width: 390, height: 844 });
  await visit(anonymous, '/catalog');
  await anonymous.page.locator('.property-card h3 a').first().waitFor();
  await anonymous.page.locator('.property-card').first().scrollIntoViewIfNeeded();
  await screenshot(anonymous, 'mobile');
  for (const [s, paths] of [[manager, ['/admin/customer-links', '/employee/viewings', '/admin/bookings', '/admin/deals', '/admin/reports', '/notifications']], [employee, ['/workspace', '/employee/clients', '/employee/viewings', '/employee/bookings', '/employee/reports', '/notifications']], [customer, ['/workspace', '/my/profile', '/my/viewings', '/my/bookings', '/notifications']], [admin, ['/admin/employees', '/admin/staff-accounts']]]) {
    for (const path of paths) await visit(s, path);
  }
  check('No uncaught browser errors', errors.length === 0);
  check('No server failures during browser navigation', serverErrors.length === 0);
} catch (error) {
  failure = safeFailure(error, [settings.ADMIN_PASSWORD, settings.SMOKE_PASSWORD, settings.SQL_PASSWORD, settings.RABBIT_PASSWORD]);
} finally {
  await browser.close();
}
const report = { status: failure ? 'Failed' : 'Passed', checks: checks.length, error: failure, results: checks };
await writeFile(new URL('result.json', evidence), JSON.stringify(report, null, 2));
console.log(JSON.stringify({ status: report.status, checks: report.checks, error: failure }));
if (failure) process.exitCode = 1;
