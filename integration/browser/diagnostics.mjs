// Playwright transport errors include request headers in their multiline call log.
// Preserve the failure category only; never persist those diagnostic headers.
export function safeFailure(error, secrets = []) {
  let message = String(error?.message ?? error)
    .replace(/\u001b\[[0-9;]*m/g, '')
    .split(/\r?\n/, 1)[0]
    .replace(/https?:\/\/[^\s"'<>]+/g, value => {
      try {
        const url = new URL(value);
        return url.origin + url.pathname + (url.search ? '?[redacted]' : '');
      } catch { return '[redacted-url]'; }
    })
    .replace(/\b(cookie|authorization|x-csrf-token)\s*[:=].*$/i, '$1: [redacted]');
  for (const secret of secrets) if (secret) message = message.split(secret).join('[redacted-secret]');
  return message.slice(0, 2000);
}
