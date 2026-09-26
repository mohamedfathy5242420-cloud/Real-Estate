# BrokerHub Angular foundation

Angular 22 standalone application for BrokerHub. F02-S01 contains the application shell,
route placeholders and hybrid rendering boundary only. Authentication, live API calls and
Business screen behavior are not implemented.

## Run locally

```bash
pnpm install --frozen-lockfile
pnpm start
```

Open `http://localhost:4200/`.

## Verify

```bash
pnpm run format:check
pnpm run build
pnpm test -- --watch=false
```

To run the built SSR server locally:

```bash
pnpm run serve:ssr:brokerhub
```

The checked-in SSR allowlist accepts `localhost` and `127.0.0.1`. A deployed host must set
`NG_ALLOWED_HOSTS` to its real comma-separated hostnames; do not use a wildcard in production.
