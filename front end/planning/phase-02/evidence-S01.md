# Evidence — F02-S01

Status: Done. Review: Codex self-review plus user acceptance through the 2026-09-26 instruction to continue with the next step.

BusinessRefs: N/A — this step creates technical application foundations and implements no Business rule.
UseCaseRefs: N/A — no use case is executed.
ScreenRefs: N/A — routes are explicit placeholders and do not claim SC completion.
AcceptanceRefs: N/A — no FAC journey is implemented.

## Delivered foundation

- Angular application under `app/`, with no nested Git repository.
- Angular CLI 22.1.8, Angular 22.1.7, Node 24.19.0, pnpm 11.19.0,
  TypeScript 6.0.3 and Vitest 4.1.11. The resolved graph is locked in `app/pnpm-lock.yaml`.
- Standalone bootstrap, routing, strict TypeScript/template checks, SSR output and Vitest.
- English LTR accessible shell and proposed CSS tokens. Placeholder text clearly says that
  feature data, authorization and API behavior are not implemented.
- Layer/type boundaries under `src/app`: `pages`, `components`, `layouts`, `services`,
  `api`, `models`, `guards`, `interceptors` and `styles`.
- Route skeleton: root redirect, two public catalog routes, three auth routes, ten
  representative private/admin routes and wildcard. SC-11 is intentionally absent.
- `RenderMode.Server` for `/catalog` and `/catalog/:unitId`; auth/private/admin routes use
  `RenderMode.Client`. Local SSR hosts are limited to `localhost` and `127.0.0.1`.
  Deployment must provide real hostnames through `NG_ALLOWED_HOSTS`.

## Acceptance evidence

| Criterion | Actual evidence | Result |
| --- | --- | --- |
| AC01 workspace/tooling | `ng version` reports CLI 22.1.8, Angular 22.1.7, Node 24.19.0, pnpm 11.19.0, TypeScript 6.0.3 and Vitest 4.1.11. `package.json`, lockfile and strict configs exist; `app/.git` was not created. | Pass |
| AC02 route skeleton | `app.routes.ts` contains the bounded placeholder routes and explicit wildcard; route pages state their incomplete status. SC-11 is excluded. | Pass |
| AC03 render boundary | `app.routes.server.ts` assigns Server to catalog/detail and Client to nonindexed groups. Unit tests assert these modes. The runtime check below proves public HTML is rendered while login HTML does not contain the client route heading. | Pass |
| AC04 structure and shell | Layer/type folders are present with boundary notes. Shell has skip link, semantic header/nav/main, visible focus treatment and English LTR document baseline. | Pass |
| AC05 real checks | Install, format, build, unit and SSR HTTP checks below ran against the generated app. | Pass |
| AC06 workflow/boundaries | Frontend validator result is recorded below. Backend and canonical Business sources were not edited. | Pass |

## Commands and results

1. Scaffold: Angular CLI `ng new brokerhub --directory app --routing --style css --ssr
   --strict --standalone --test-runner vitest --package-manager pnpm --skip-git
   --skip-install --defaults` — exit 0.
2. Install: final `pnpm install --frozen-lockfile` — exit 0. pnpm's current
   `allowBuilds` configuration permits only `@parcel/watcher`, `esbuild`, `lmdb` and
   `msgpackr-extract`. An initial install rejected ignored build scripts; the allowlist
   replaced that incomplete attempt and the frozen install completed.
3. Formatting: `node_modules/.bin/prettier.CMD --check .` — exit 0,
   `All matched files use Prettier code style!`.
4. Production build: `node_modules/.bin/ng.CMD build` — exit 0. Browser initial total
   259.22 kB raw / 72.63 kB estimated transfer; server bundles generated; zero static
   prerenders, as both public routes are server-rendered. Output: `app/dist/brokerhub`.
5. Unit tests: `node_modules/.bin/ng.CMD test --watch=false` — exit 0,
   2 test files and 4 tests passed. The first test attempt exposed a missing Router
   provider in the shell test; `provideRouter([])` corrected the test harness before the
   recorded pass.
6. SSR runtime: built server on port 4100, then Node `fetch`:
   - `/catalog` → 200, HTML length 6201, contains `Property catalog`.
   - `/catalog/demo-unit` → 200, HTML length 6210, contains `Unit details`.
   - `/auth/login` → 200, HTML length 1082, does not contain `Customer sign in`, which
     confirms the client-rendered boundary rather than leaking the route page into SSR HTML.
   The first runtime attempt correctly rejected `localhost` because the generated
   `allowedHosts` list was empty; the local-only allowlist fixed that finding before this pass.
7. Workflow: `powershell -NoProfile -ExecutionPolicy Bypass -File
   "D:/Practice/front end/scripts/validate.ps1"` — exit 0: 54 required files present and
   nonempty; state/handoff/step consistent; at most one InProgress step; 16 BR, 10 UC,
   10 SC and 10 FAC linked; 7/7 Business snapshots match the canonical source.

Browser visual/journey testing: Not run — this step verifies the technical shell and rendering
boundary; no Business journey is implemented. Backend integration: N/A — no live API or
contract adapter exists in this step. Google indexing: Not claimed; SSR is only the technical
foundation for FE-D009.

## Self-review

- Scope stayed within `front end`; no Backend source or canonical Business file was changed.
- No fake API, auth success, permissions or final enum/state behavior was introduced.
- Public SSR output contains placeholder public content only; auth/private route bodies remain
  client-rendered.
- The application builds and tests. The user accepted the handoff by authorizing the next step
  on 2026-09-26; this closes S01 without approving any open Business/API decision.

Official references checked on 2026-09-24: Angular version compatibility
(`https://angular.dev/reference/versions`), CLI `ng new` options
(`https://angular.dev/cli/new`) and rendering strategies
(`https://angular.dev/guide/routing/rendering-strategies`).
