# Evidence — P02-S03
Status: InReview
Review type: self-review PASS; not independent approval.

Scope: SQL Server Identity persistence, cookie authentication plumbing, current-user HTTP adapter
and first migration only. Account lifecycle endpoints and Customer linking are outside this step.

## Implemented files

- `Directory.Packages.props`: Identity/EF Core SQL Server/Design 10.0.12.
- `.config/dotnet-tools.json`: repository-local `dotnet-ef` 10.0.12.
- `src/BrokerHub.Infrastructure/Identity/ApplicationUser.cs` and identity registration.
- `src/BrokerHub.Infrastructure/Persistence/ApplicationDbContext.cs` and SQL Server registration.
- `src/BrokerHub.Infrastructure/Persistence/Migrations/*InitialIdentity*` plus model snapshot.
- `src/BrokerHub.Api/Identity/HttpCurrentUser.cs`, composition/middleware updates and development
  SQL Express connection with Windows authentication and no credential in source.

## L1 — structure, packages and build

- `dotnet restore BrokerHub.sln`: exit 0; all eight projects restored.
- `dotnet tool restore`: exit 0; `dotnet-ef` 10.0.12 restored.
- `dotnet build BrokerHub.sln --no-restore`: exit 0; eight projects, 0 warnings, 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\validate.ps1`: exit 0; 90 required
  files, state/handoff/step consistent, at most one InProgress step, 16 BR / 10 UC / 14 AC.
- Manual dependency inspection: Domain still has no project/package reference; Application still
  references Domain plus DI abstractions only. EF/Identity/SQL Server are Infrastructure concerns;
  HTTP claims adapter is API-only.
- `dotnet list ... package` confirms Infrastructure resolves Identity EF, EF Design and SQL Server
  at 10.0.12; API resolves EF Design 10.0.12 only for tooling.

## L2 — decisions and Business boundary

- D033/D034: Identity email/password configuration requires unique and confirmed email; cookie is
  HttpOnly, SameSite Lax, always Secure, 8-hour sliding lifetime. API redirects become 401/403.
- D035: no Customer entity, phone/email match or automatic linking was added. That reviewed workflow
  is still future work.
- Existing `AppRoles` remains the only list of four application roles; this step seeds no role/user.
- Q07 former-assignee behavior is absent. Role policies still do not replace ownership/assignment.

## L3 — migration and real SQL Server

- Migration generation: `dotnet ef migrations add InitialIdentity ... -- --environment Development`;
  build succeeded and generated `20260926133217_InitialIdentity` plus snapshot.
- Migration update: `dotnet ef database update ... -- --environment Development`; build succeeded.
- Independent `sqlcmd` inspection against `.\\SQLEXPRESS`: exit 0. Database `BrokerHub` exists;
  `dbo.__EFMigrationsHistory` records `20260926133217_InitialIdentity` / product `10.0.12`.
- Actual tables in schema `Identity`: `Accounts`, `Roles`, `AccountRoles`, `AccountClaims`,
  `AccountLogins`, `AccountTokens`, `RoleClaims`. No Customer/Employee/Business table exists.

## L4 — live smoke

- API started temporarily on `http://127.0.0.1:5082`.
- `GET /healthz`: HTTP 200, body `Healthy`; temporary process stopped.
- Registration/login/email/recovery journeys: N/A in S03; explicitly assigned to P02-S04.
- Automated tests: N/A / deferred by accepted D031; no `dotnet test` claim.

## Acceptance self-review

- SA01 PASS: Domain/Application are free from EF, SQL Server and HTTP claims implementation.
- SA02 PASS: Guid Identity keys; no user/role seed and no invented role.
- SA03 PASS: cookie security configuration is explicit and no browser token storage exists.
- SA04 PASS: Identity requires confirmed email; account endpoints are absent.
- SA05 PASS: Identity-only migration applied and inspected on local SQL Express.
- SA06 PASS: restore/build/database/health passed; final validator result recorded below.
- SA07 PASS: no auto-link, Q07 rule, Business endpoint, frontend edit or automated test.

Self-review verdict: PASS. Step remains InReview for the user's review; no independent review is claimed.
