import { readdir, readFile, stat } from 'node:fs/promises';
import { deflateSync } from 'node:zlib';
import { request as nodeRequest } from 'node:http';

const base = process.env.BASE_URL ?? 'http://gateway';
const origin = process.env.PUBLIC_ORIGIN ?? 'http://localhost:8080';
const password = process.env.SMOKE_PASSWORD;
const smokeEmail = process.env.SMOKE_EMAIL ?? 'smoke@brokerhub.test';
if (!password) throw new Error('SMOKE_PASSWORD must be configured privately.');
if (!/^[^\s@]+@[^\s@]+\.(test|invalid)$/.test(smokeEmail)) throw new Error('Use an explicitly allowlisted local test address.');
let attempts = 0;
const checks = [];
function check(label, condition) {
  checks.push({ label, passed: Boolean(condition) });
  if (!condition) throw new Error(`Check failed: ${label}`);
}
function session() { return { cookies: new Map(), token: null }; }
async function request(path, { method = 'GET', body, user, headers = {} } = {}) {
  attempts++;
  const outgoing = { Host: new URL(origin).host, ...headers };
  if (user?.cookies.size) outgoing.Cookie = [...user.cookies].map(([key, value]) => `${key}=${value}`).join('; ');
  if (method !== 'GET') outgoing.Origin ??= origin;
  if (body !== undefined && !(body instanceof FormData)) outgoing['Content-Type'] = 'application/json';
  let payload;
  if (body instanceof FormData) {
    const encoded = new Response(body);
    outgoing['Content-Type'] = encoded.headers.get('content-type');
    payload = Buffer.from(await encoded.arrayBuffer());
  } else if (body !== undefined) payload = Buffer.from(JSON.stringify(body));
  // Node24 fetch ignores an explicit Host header. Native HTTP preserves the real
  // browser Host while routing to Docker DNS, without weakening Angular's allowlist.
  const response = await new Promise((resolve, reject) => {
    const connection = nodeRequest(base + path, { method, headers: outgoing, signal: AbortSignal.timeout(60000) }, incoming => {
      const chunks = [];
      incoming.on('data', chunk => chunks.push(chunk));
      incoming.on('error', reject);
      incoming.on('end', () => {
        const headers = new Headers();
        for (const [key, value] of Object.entries(incoming.headers)) {
          for (const item of Array.isArray(value) ? value : [value]) if (item !== undefined) headers.append(key, item);
        }
        resolve(new Response([204,304].includes(incoming.statusCode) ? null : Buffer.concat(chunks), { status: incoming.statusCode, headers }));
      });
    });
    connection.on('error', reject); connection.end(payload);
  });
  for (const cookie of response.headers.getSetCookie()) {
    const [name, ...value] = cookie.split(';', 1)[0].split('=');
    if (user) { if (value.join('=')) user.cookies.set(name, value.join('=')); else user.cookies.delete(name); }
  }
  const text = await response.text();
  let data; try { data = JSON.parse(text); } catch { data = null; }
  return { response, text, data };
}
async function csrf(user) {
  const result = await request('/api/session/antiforgery', { user });
  check('Anonymous/current-session CSRF bootstrap', result.response.status === 200 && result.data?.headerName === 'X-CSRF-TOKEN' && result.data?.token);
  user.token = result.data.token;
}
async function post(path, body, user, expected = 200) {
  if (!user.token) await csrf(user);
  const result = await request(path, { method: 'POST', body, user, headers: { 'X-CSRF-TOKEN': user.token } });
  check(`POST ${path.replace(/[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}/gi, ':id')} -> ${expected}`, result.response.status === expected);
  return result.data;
}
async function captured(kind, recipient) {
  const paths = await readdir('/capture');
  const entries = await Promise.all(paths.filter(name => /^[a-f0-9]{32}\.json$/.test(name)).map(async name => {
    const path = '/capture/' + name;
    return { modified: (await stat(path)).mtimeMs, value: JSON.parse(await readFile(path, 'utf8')) };
  }));
  const record = entries.filter(entry => entry.value.Kind === kind && entry.value.Recipient === recipient).sort((a, b) => b.modified - a.modified)[0];
  check(`Private local ${kind} capture exists (no SMTP)`, record?.value?.Link);
  return new URL(record.value.Link);
}
function png() {
  function crc(buffer) { let value = 0xffffffff; for (const byte of buffer) { value ^= byte; for (let j = 0; j < 8; j++) value = (value >>> 1) ^ ((value & 1) ? 0xedb88320 : 0); } return (value ^ 0xffffffff) >>> 0; }
  function chunk(type, bytes) { const name = Buffer.from(type); const result = Buffer.alloc(bytes.length + 12); result.writeUInt32BE(bytes.length); name.copy(result, 4); bytes.copy(result, 8); result.writeUInt32BE(crc(Buffer.concat([name, bytes])), bytes.length + 8); return result; }
  const header = Buffer.alloc(13); header.writeUInt32BE(16); header.writeUInt32BE(16, 4); header[8] = 8; header[9] = 2;
  const pixels = Buffer.alloc((16 * 3 + 1) * 16);
  for (let y = 0; y < 16; y++) for (let x = 0; x < 16; x++) { const offset = y * 49 + 1 + x * 3; pixels.set([70, 89, 65], offset); }
  return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]), chunk('IHDR', header), chunk('IDAT', deflateSync(pixels)), chunk('IEND', Buffer.alloc(0))]);
}
async function seedCatalog() {
  const admin = session(); await csrf(admin);
  await post('/api/accounts/login', { email: process.env.ADMIN_EMAIL, password: process.env.ADMIN_PASSWORD, rememberMe: false }, admin);
  await csrf(admin);
  const code = 'DOCKER-INTEGRATION';
  const developers = await request('/api/developers', { user: admin });
  let developer = developers.data?.find(item => item.code === code);
  if (!developer) developer = await post('/api/developers', { code, name: 'Test developer Docker — Fictional data', legalName: null, externalContactName: null, externalContactEmail: null, externalContactPhone: null }, admin, 201);
  const projects = await request(`/api/projects?developerId=${developer.id}`, { user: admin });
  let project = projects.data?.find(item => item.code === code);
  if (!project) project = await post('/api/projects', { developerId: developer.id, code, name: 'Integration test project', area: 'New Cairo', address: 'Illustrative test address only', timeZoneId: 'Africa/Cairo' }, admin, 201);
  const agreements = await request(`/api/marketing-agreements?developerId=${developer.id}`, { user: admin });
  let agreement = agreements.data?.find(item => item.agreementNumber === code);
  if (!agreement) agreement = await post('/api/marketing-agreements', { developerId: developer.id, agreementNumber: code, scopeType: 'AllDeveloperProjects', startsOn: '2020-01-01', endsOn: '2099-12-31', protectionPeriodDays: 90, projectIds: [], commissionRules: [{ projectId: null, ruleType: 'Percentage', percentage: '2.00', fixedAmount: null, currency: 'EGP' }] }, admin, 201);
  if (agreement.status === 'Draft') await post(`/api/marketing-agreements/${agreement.id}/activate`, { version: agreement.version }, admin);
  const units = await request(`/api/units?projectId=${project.id}`, { user: admin });
  let unit = units.data?.find(item => item.code === code);
  if (!unit) unit = await post('/api/units', { projectId: project.id, code, title: 'Integration test apartment — Fictional data', description: 'Listing for integration testing Angular with the server inside Docker. This is not a real property offer.', unitType: 'Apartment', areaSqm: '150.25', bedrooms: 3, bathrooms: 2, listPrice: '1234567.89', currency: 'EGP' }, admin, 201);
  const existingImages = await request(`/api/units/${unit.id}/images`, { user: admin });
  if (!existingImages.data?.images?.length) {
    const upload = new FormData(); upload.set('File', new Blob([png()], { type: 'image/png' }), 'integration-test.png');
    upload.set('AltText', 'Color sample for image testing; not a property photograph'); upload.set('SortOrder', '0'); upload.set('Version', unit.version);
    const image = await request(`/api/units/${unit.id}/images`, { method: 'POST', body: upload, user: admin, headers: { 'X-CSRF-TOKEN': admin.token } });
    check('Linux SkiaSharp validates and stores a real PNG', image.response.status === 201);
    unit = (await request(`/api/units/${unit.id}`, { user: admin })).data;
  }
  if (unit.publicationStatus !== 'Published') await post(`/api/units/${unit.id}/publish`, { version: unit.version, reason: 'Local integration testing only' }, admin);
  await post('/api/accounts/logout', {}, admin, 204);
  return unit.id;
}

try {
  check('API liveness through gateway', (await request('/healthz')).response.status === 200);
  check('SQL/migrations/private-storage readiness through gateway', (await request('/readyz')).response.status === 200);
  check('Anonymous session is rejected', (await request('/api/accounts/me')).response.status === 401);
  const listing = await request('/api/catalog/units');
  check('Public catalog is an actual API page', listing.response.status === 200 && Array.isArray(listing.data?.items));
  check('API data is no-store', listing.response.headers.get('cache-control')?.includes('no-store'));
  const badPost = await request('/api/accounts/register', { method: 'POST', body: { email: smokeEmail, password, phoneNumber: '+201000000001' } });
  check('Unsafe anonymous API request requires CSRF', badPost.response.status === 400);

  const user = session(); await csrf(user);
  const registered = await request('/api/accounts/register', { method: 'POST', body: { email: smokeEmail, password, phoneNumber: '+201000000001' }, user, headers: { 'X-CSRF-TOKEN': user.token } });
  check('Register new local customer or reuse this smoke account', [202,400].includes(registered.response.status));
  let needsConfirmation = registered.response.status === 202;
  if (needsConfirmation) {
    check('Unconfirmed customer cannot log in', (await request('/api/accounts/login', { method: 'POST', body: { email: smokeEmail, password }, user, headers: { 'X-CSRF-TOKEN': user.token } })).response.status === 403);
  } else {
    const existing = await request('/api/accounts/login', { method: 'POST', body: { email: smokeEmail, password }, user, headers: { 'X-CSRF-TOKEN': user.token } });
    check('Reused smoke account has a known login state', [200,401,403].includes(existing.response.status));
    needsConfirmation = existing.response.status === 403;
    if (existing.response.status === 200) { await csrf(user); await post('/api/accounts/logout', {}, user, 204); await csrf(user); }
    if (needsConfirmation) await post('/api/accounts/resend-confirmation', { email: smokeEmail }, user, 202);
  }
  if (needsConfirmation) {
    const confirmation = await captured('confirmation', smokeEmail);
    check('Backend confirmation link targets the actual Angular route/origin', confirmation.origin === origin && confirmation.pathname === '/auth/confirm-email');
    await post('/api/accounts/confirm-email', { accountId: confirmation.searchParams.get('accountId'), token: confirmation.searchParams.get('token') }, user, 204);
  }
  // Reset makes reruns deterministic without touching another account or printing links/tokens.
  await post('/api/accounts/forgot-password', { email: smokeEmail }, user, 202);
  const reset = await captured('password-reset', smokeEmail);
  check('Backend reset link targets Angular', reset.origin === origin && reset.pathname === '/auth/reset-password');
  await post('/api/accounts/reset-password', { email: smokeEmail, token: reset.searchParams.get('token'), newPassword: password }, user, 204);
  const oldToken = user.token;
  await post('/api/accounts/login', { email: smokeEmail, password, rememberMe: false }, user);
  const me = await request('/api/accounts/me', { user });
  check('Current customer session is real and role-restricted', me.response.status === 200 && me.data?.roles?.length === 1 && me.data.roles[0] === 'Customer');
  const oldPair = await request('/api/accounts/logout', { method: 'POST', body: {}, user, headers: { 'X-CSRF-TOKEN': oldToken } });
  check('Anonymous CSRF token cannot be reused after login', oldPair.response.status === 400);
  await csrf(user);
  const forbidden = await request('/api/staff-accounts', { method: 'POST', body: { email: 'employee@brokerhub.test', password, phoneNumber: '+201000000002', role: 'SalesEmployee' }, user, headers: { 'X-CSRF-TOKEN': user.token } });
  check('Customer cannot provision staff', forbidden.response.status === 403);
  const hostile = await request('/api/accounts/logout', { method: 'POST', body: {}, user, headers: { 'X-CSRF-TOKEN': user.token, Origin: 'https://hostile.invalid' } });
  check('Hostile unsafe Origin is rejected', hostile.response.status === 403);
  const publicHtml = await request('/catalog', { user });
  check(`Public SSR returns HTTP200 (actual ${publicHtml.response.status})`, publicHtml.response.status === 200);
  check('Angular SSR public HTML does not serialize the logged-in email or CSRF token', !publicHtml.text.includes(smokeEmail) && !publicHtml.text.includes(user.token));
  await post('/api/accounts/logout', {}, user, 204);
  check('Logout invalidates session', (await request('/api/accounts/me', { user })).response.status === 401);

  const unitId = await seedCatalog();
  const detail = await request(`/api/catalog/units/${unitId}`);
  check('Live catalog detail keeps exact decimal strings', detail.response.status === 200 && detail.data?.listPrice === '1234567.89' && detail.data?.areaSqm === '150.25');
  check('Public detail contains no internal codes/versions/notes', !('version' in detail.data) && !('code' in detail.data) && !('lastAvailabilitySource' in detail.data));
  const image = await request(detail.data.images[0].contentUrl);
  check('Public PNG content traverses the gateway', image.response.status === 200 && image.response.headers.get('content-type') === 'image/png');
  const html = await request(`/catalog/${unitId}`);
  check('Actual API property and exact English price are in SSR HTML', html.response.status === 200 && html.text.includes(detail.data.title) && html.text.includes('1,234,567.89'));
  check('Live SSR HTML is not cached', html.response.headers.get('cache-control')?.includes('no-store'));
  const found = await request('/catalog?q=' + encodeURIComponent('Integration test'));
  check('SSR search uses real API results', found.response.status === 200 && found.text.includes(detail.data.title));
  const missing = await request('/catalog/10000000-0000-0000-0000-000000000099');
  check('Unknown unit returns actual HTTP404 and not-found UI', missing.response.status === 404 && missing.text.includes('Unit not found'));
  const loginHtml = await request('/auth/login');
  check('Private account route is a CSR shell without sample credentials', loginHtml.response.status === 200 && !loginHtml.text.includes(password));
  console.log(JSON.stringify({ status: 'Passed', checks: checks.length, httpAttempts: attempts, fixtureUnitId: unitId, localFictionalFixtureRetained: true, results: checks }, null, 2));
} catch (error) {
  console.error(JSON.stringify({ status: 'Failed', checks: checks.length, httpAttempts: attempts, error: error.message, results: checks }, null, 2));
  process.exitCode = 1;
}
