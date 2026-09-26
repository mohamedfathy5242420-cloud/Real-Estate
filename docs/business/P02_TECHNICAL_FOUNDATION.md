# P02 technical foundation plan

Status: Active implementation plan. P02-S01/S02 are Done; P02-S03 implements the accepted
SQL Server/Identity foundation in D033–D036.
The design honors D002: Clean Architecture by layer/type, without Vertical Slice.

## Technology baseline

| Concern | Proposed choice | Reason / business link |
| --- | --- | --- |
| Runtime | .NET 10 LTS, SDK 10.0.401, TargetFramework net10.0 (D032) | Verified on the machine and pinned by global.json |
| HTTP | ASP.NET Core controller-based Web API with OpenAPI | Explicit resource/use-case endpoints and Angular contract; UC-01–10 |
| Database | Microsoft SQL Server | Chosen by the user in D036; transactions, filtered indexes and local SQL Express support |
| ORM/migrations | EF Core provider matching the pinned .NET major | Mapping, migrations and optimistic concurrency while keeping Domain persistence-free |
| Validation | Application validators plus Domain invariants; package selected during scaffold | Clear request errors without putting business truth only in controllers |
| CQRS/Mediator | Commands, Queries, Handlers and pipeline behaviors grouped by type in Application | Applies course CQRS/Mediator without feature/Vertical-Slice folders |
| Persistence | Focused repository interfaces only where an aggregate needs them; query services for reads; Unit of Work/SaveChanges at command boundary | Avoids a generic repository and premature SaveChanges; BR-15 concurrency/audit |
| Testing | Four xUnit-style project scaffolds exist; automated tests are deferred by D031 to a later teaching step | Future coverage targets Domain rules, dependency direction and real constraints; AC-01–14 |
| Messaging | Transactional Outbox; RabbitMQ adapter introduced in P08 | Notification failure cannot lose core operation (AC-14) |
| Cache | Adapter introduced with catalog reads; distributed cache candidate in P08 | Cache may accelerate catalog but never confirm booking (BR-16) |
| Channels | In-process bounded channels only for non-authoritative background work | Never the durable store for reservation/commission events |
| Observability | Structured logs, correlation IDs, health checks, metrics/traces through standard .NET abstractions | Diagnose distributed retries/audit without coupling Domain |
| Time | `TimeProvider` abstraction; UTC persistence; IANA time-zone IDs at project/viewing boundary | Deterministic Q03 tests and correct display |

Package names/versions are deliberately not locked in P01. P02-S01 records the installed SDK,
dependency license/version check and repeatable restore before committing package choices.

## Solution and project structure

```text
BrokerHub.sln
src/
  BrokerHub.Domain/
    Entities/
    Enums/
    ValueObjects/
    Events/
    Exceptions/
  BrokerHub.Application/
    DTOs/
    Interfaces/
    Commands/
    Queries/
    Handlers/
    Validators/
    Services/
    Facades/
    Orchestrators/
    Behaviors/
    Mappings/
  BrokerHub.Infrastructure/
    Persistence/
      Configurations/
      Repositories/
      Migrations/
    Identity/
    Messaging/
    Caching/
    Time/
  BrokerHub.Api/
    Controllers/
    Middleware/
    Contracts/
    Configuration/
tests/
  BrokerHub.Domain.Tests/
  BrokerHub.Application.Tests/
  BrokerHub.Architecture.Tests/
  BrokerHub.IntegrationTests/
```

Dependencies:

```text
Domain <- Application <- Infrastructure
             ^              ^
             +------ API ---+
```

- Domain references no other project or framework persistence type.
- Application references Domain and declares ports/interfaces.
- Infrastructure implements Application ports and maps persistence/identity/messaging.
- API composes dependencies and exposes HTTP; controllers contain no business rules.
- Tests reference only layers needed for their purpose.
- No `Features/<feature>` vertical slices. Commands/queries/handlers remain type-organized.

## Application patterns and course topics

- CQRS separates state-changing commands from read projections; it does not require separate databases.
- Mediator dispatches handlers and cross-cutting validation/authorization/idempotency behaviors.
- Facades expose cohesive application capabilities to controllers where several query/services must
  be coordinated without leaking internals.
- Orchestrators own multi-step workflows such as external reservation response and deal adoption.
- Repositories load/save aggregate state; query services return DTO projections.
- One application command owns SaveChanges/transaction. Repositories do not commit.
- Outbox provides durable integration events; RabbitMQ consumers are idempotent.
- Cache decorates safe catalog/query paths. Writes and external confirmations bypass cached authority.
- Channels may batch local metrics/email preparation after durable outbox acquisition.

## Cross-cutting behavior order

Proposed command pipeline:

1. Correlation/context.
2. Authentication and current resource authorization.
3. Input validation.
4. Idempotency receipt lookup/acquisition for applicable commands.
5. Handler/orchestrator with optimistic concurrency and one transaction.
6. Audit and Outbox saved in the same transaction.
7. Response mapping; asynchronous delivery after commit.

Authorization must check role plus ownership/current assignment. Angular route guards are user
experience only. Customer DTOs never select internal SalesNote data (BR-06/09).

## Identity decision — Q01/Q05

D033–D035 select ASP.NET Core Identity, email/password and an HttpOnly Secure cookie for the
Angular browser. Customers self-register; staff accounts are provisioned by System Administrator.
Email confirmation gates viewing/reservation commands and password recovery uses email.
Domain Customer remains independent from auth Account. Similar phone/email data never auto-links;
a verified account-link request requires manager review and audit.

## Persistence mapping order

1. Catalog and agreement mapping, including uniqueness and agreement overlap service.
2. Customer/Employee/Assignment/Lead.
3. Viewing plus interval constraint and schedule revisions.
4. Reservation/cancellation/deposit.
5. Deal/commission snapshot/collections.
6. Audit, CommandReceipt and Outbox.

Each feature phase adds its migration with the behavior; automated tests are introduced through the
separate learning step required by D031. P02 foundation should not create
the entire schema before aggregate implementation; it establishes conventions and a minimal database
connection/migration smoke path.

## First coding step — P02-S01

Outcome: a buildable empty foundation with enforced dependency direction.

Deliverables:

1. Record actual installed SDK; choose/pin supported LTS via `global.json`.
2. Create solution and the four `src` plus four `tests` projects above.
3. Add only allowed project references and verify them manually; retain an empty Architecture.Tests
   scaffold for the later D031 testing step.
4. Add central build settings, nullable/implicit-using policy and warnings policy.
5. Keep all four test projects empty/buildable and remove fake template tests (D031).
6. Add API health endpoint and composition skeleton without business endpoints.
7. Add configuration/options validation and placeholder infrastructure registration.
8. Document restore/build and health-check outputs; mark automated tests deferred by D031.

Not part of P02-S01: auth feature, full database schema, RabbitMQ/Redis deployment, business endpoint,
Angular integration, generated fake implementation evidence.

## Later phase allocation

| Phase | Foundation usage |
| --- | --- |
| P02 | solution, dependency enforcement, identity decision/authorization primitives |
| P03 | catalog, agreement scope/rules, public queries |
| P04 | customer, assignment, Lead, internal notes |
| P05 | Viewing intervals, reschedule history, outcome privacy |
| P06 | reservations, developer snapshots, idempotency/concurrency |
| P07 | Deal and commission snapshot/collection |
| P08 | Outbox/RabbitMQ notifications, caching and Channels |
| P09 | Angular contract integration and end-to-end acceptance |

## Decisions required before affected implementation

- Q04 before automatic freshness/hiding jobs.
- Q06 before confirmed cancellation/refund transitions.
- Q07 before former-assignee query authorization.
- Booking response timeout, linked reservation completion and Unit state after sale before those
  handlers/constraints are finalized.

These seams do not block P02-S01 scaffolding.
