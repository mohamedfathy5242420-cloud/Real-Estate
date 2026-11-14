import { test } from 'node:test';
import assert from 'node:assert/strict';
import { safeFailure } from './diagnostics.mjs';
test('transport failure keeps its category and drops multiline Cookie/Authorization/CSRF call logs', () => {
  const result = safeFailure(new Error('apiRequestContext.fetch: connect ECONNREFUSED 127.0.0.1:8080\nCall log:\n - cookie: private-session\n - Authorization: private-auth\n - X-CSRF-TOKEN: private-csrf'));
  assert.equal(result, 'apiRequestContext.fetch: connect ECONNREFUSED 127.0.0.1:8080');
  assert.doesNotMatch(result, /private-/);
});
test('single-line headers, URL queries, known secrets and ANSI are redacted', () => {
  assert.equal(safeFailure('Oops Cookie: private-session'), 'Oops Cookie: [redacted]');
  assert.equal(safeFailure('\u001b[2msecret https://localhost/example?token=private\u001b[22m', ['secret']), '[redacted-secret] https://localhost/example?[redacted]');
});
