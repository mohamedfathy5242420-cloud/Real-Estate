# Evidence — P02-S01

Status: Done
Evidence revision: 2026-09-23
Review type: self-review; not independent approval.

## L1 — Structural validation

Command from `D:/Practice`:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```

Actual result:
```
PASS: 51 required workflow files are present and nonempty.
PASS: current state, handoff and active step are consistent.
PASS: at most one implementation step is InProgress.
Scope: document structure only. Manual review and application tests are NOT implied.
PASS: Business linkage - 16 rules, 10 use cases, 14 acceptance scenarios.
```

Exit code: 0

### L1 rerun after solution/project scaffolding

Actual result / exit code 0:
```
PASS: 61 required workflow files are present and nonempty.
PASS: current state, handoff and active step are consistent.
PASS: at most one implementation step is InProgress.
PASS: Business linkage - 16 rules, 10 use cases, 14 acceptance scenarios.
```

### L1 rerun after centralized build/package settings

Actual result / exit code 0:
```
PASS: 64 required workflow files are present and nonempty.
PASS: current state, handoff and active step are consistent.
PASS: at most one implementation step is InProgress.
PASS: Business linkage - 16 rules, 10 use cases, 14 acceptance scenarios.
```

### L1 rerun after API composition and health endpoint

Actual result / exit code 0:
```
PASS: 74 required workflow files are present and nonempty.
PASS: current state, handoff and active step are consistent.
PASS: at most one implementation step is InProgress.
PASS: Business linkage - 16 rules, 10 use cases, 14 acceptance scenarios.
```

## L2 — SDK and build commands

### dotnet --info (recorded at step start)
```
.NET SDK: 10.0.401
.NET Runtimes: Microsoft.AspNetCore.App 8.0.27, 10.0.12; Microsoft.NETCore.App 8.0.27, 10.0.12
global.json: Not found
```

### global.json decision
P02_TECHNICAL_FOUNDATION.md recommends pinning a supported LTS. The machine has 8.0.421 (LTS, near end of support) and 10.0.401 (LTS, active support). 
Decision: .NET 10 SDK 10.0.401 is pinned by `global.json`; all eight generated projects target net10.0.

### Foundation batch 2 commands
- `dotnet new sln --name BrokerHub --format sln`
- `dotnet new classlib` for Domain, Application and Infrastructure
- `dotnet new web` for Api
- `dotnet new xunit` for the four test projects
- `dotnet sln BrokerHub.sln add ...` for all eight projects
- `dotnet add ... reference ...` for the allowed dependency graph

All scaffold commands completed successfully. Templates were created with `--no-restore`;
restore/build/test intentionally remain for a later S01 batch.

### Commands executed
| Command | Exit code | Notes |
| --- | --- | --- |
| `dotnet restore BrokerHub.sln` | 0 | First sandboxed attempt failed because NuGet.Config access was denied; approved rerun outside the sandbox restored all eight projects |
| `dotnet build BrokerHub.sln --no-restore` | 0 | Eight projects built; 0 warnings and 0 errors |
| `dotnet test` | N/A | Deferred by accepted decision D031; no claim that tests ran |

Actual build summary:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Centralization checks:
- PASS: no `.csproj` contains an inline NuGet package version.
- PASS: TargetFramework, Nullable, ImplicitUsings and TreatWarningsAsErrors are not
  duplicated in project files.
- MSBuild evaluated the shared values as net10.0 / enable / enable / true.

### API composition and health batch
- Added `AddApplication()` and `AddInfrastructure()` composition entry points.
- Infrastructure exposes empty registration seams for Persistence, Identity, Messaging and
  Caching; Time registers `TimeProvider.System` through the standard abstraction.
- Added and validated the required `BrokerHub:ApplicationName` options section at startup.
- Removed the generated `Class1.cs` files and the template root `Hello World` endpoint.
- Added controller routing and `GET /healthz` only; no Business endpoint was introduced.
- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12 is centrally pinned.
- Restore exit 0 and build exit 0; build output: 0 warnings, 0 errors.

## L3 — Dependency direction verification

Dependency rules reserved for the later testing step:
- Domain references no other project and no Infrastructure/Application/Api types
- Application references only Domain
- Infrastructure references Application (and Domain transitively)
- Api references Application and Infrastructure (not Domain directly)
- Test projects reference only their target layer

Current project-reference inspection: PASS. A PowerShell XML check compared every `.csproj`
reference set with the allowed graph and confirmed that `BrokerHub.sln` contains eight projects.
The first ad-hoc checker included null entries from empty ItemGroups; its filtering was corrected
and the rerun passed without changing project files.
- Domain: no project reference.
- Application: Domain only.
- Infrastructure: Application only.
- Api: Application and Infrastructure only.
- Domain.Tests: Domain only; Application.Tests: Application only.
- Architecture.Tests: all four source assemblies for dependency inspection.
- IntegrationTests: Api only.

Automated architecture-rule behavior is deferred by D031. The four empty test projects remain
in the solution so tests can be introduced as a separately explained learning step.

## L4 — Health endpoint

`dotnet run --project src/BrokerHub.Api/BrokerHub.Api.csproj --no-build --urls
http://127.0.0.1:5080` started successfully. A real HTTP request returned:
```
StatusCode: 200
Content: Healthy
Content-Type: text/plain
```
The temporary local server was stopped cleanly after the check.

## Acceptance criteria and limits

- SA01: build succeeds with warnings-as-errors — Passed for the current scaffold (0 warnings, 0 errors)
- SA02: four empty test projects build; fake template tests removed; dependency graph manually checked — Passed under D031
- SA03: health endpoint 200 OK — Passed by live localhost smoke check
- SA04: global.json + Directory.Build.props + Directory.Packages.props present and correct — Passed
- SA05: no business code, migrations, auth, RabbitMQ, Redis, Angular, Vertical Slice — Passed for the current batch
- SA06: evidence/handoff sufficient — Passed for InReview handoff

Not run / N/A: automated tests are deferred by D031; business behavior tests, database
migration, authentication, messaging, caching and Angular contract are outside S01 scope.

## Created files

- global.json
- BrokerHub.sln
- Directory.Build.props
- Directory.Packages.props
- .editorconfig
- src/BrokerHub.Domain/BrokerHub.Domain.csproj
- src/BrokerHub.Application/BrokerHub.Application.csproj
- src/BrokerHub.Infrastructure/BrokerHub.Infrastructure.csproj
- src/BrokerHub.Api/BrokerHub.Api.csproj
- tests/BrokerHub.Domain.Tests/BrokerHub.Domain.Tests.csproj
- tests/BrokerHub.Application.Tests/BrokerHub.Application.Tests.csproj
- tests/BrokerHub.Architecture.Tests/BrokerHub.Architecture.Tests.csproj
- tests/BrokerHub.IntegrationTests/BrokerHub.IntegrationTests.csproj
- Template source/configuration files generated by `dotnet new`; no Business implementation.
- src/BrokerHub.Application/DependencyInjection.cs
- src/BrokerHub.Infrastructure/DependencyInjection.cs and the five registration seams
- src/BrokerHub.Api/Configuration/BrokerHubOptions.cs
- updated src/BrokerHub.Api/Program.cs and appsettings.json
- API Program.cs, health endpoint and DI composition skeleton
- Placeholder infrastructure registration extensions and TimeProvider registration

## Changed files for this step
- planning/phase-02/README.md
- planning/phase-02/steps/S01.md
- planning/phase-02/evidence-S01.md
- docs/CURRENT_STATE.md
- handoffs/CURRENT_HANDOFF.md
- scripts/validate.ps1 (updated to check P02 planning and scaffold files)
- global.json, BrokerHub.sln and the eight project scaffolds listed above
- Directory.Build.props, Directory.Packages.props and .editorconfig
- all eight `.csproj` files (shared properties/package versions centralized)
- Application/Infrastructure DI composition, API options and health endpoint files

## Review decision

Self-review: PASS against the revised D031 scope. The user authorized proceeding to the next
step on 2026-09-24; no independent-agent review or automated-test execution is claimed.
