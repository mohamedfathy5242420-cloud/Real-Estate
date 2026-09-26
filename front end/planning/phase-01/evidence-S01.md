# Evidence — F01-S01

Historical note (2026-09-23): This step was completed before canonical D015–D030 were accepted. Its Q02/Q03 Open/Proposed references describe the state at completion, not the current Business state. F01-S02 and the updated shared snapshots use Q02/Q03 as Accepted. The proposed booking response-wait timeout is a separate unresolved policy, not Q03. Later decisions supersede FE-A002 with English LTR (FE-D007), select customer email/password input (FE-D008; canonical Q01 still Open) and require public indexing (FE-D009); use current DECISIONS/API_CONTRACT for these statuses.

## Verification Commands and Results

### Script validation (L1 Structure)
```
Command: powershell -NoProfile -ExecutionPolicy Bypass -File "D:/Practice/front end/scripts/validate.ps1"
Result: exit 0; 37 required workflow files present/nonempty; state/handoff/active-step consistent; at most one InProgress; linkage 16 BR, 10 UC, 10 SC, 10 FAC; PASS: 7 Business snapshots match canonical source.
```

### Screen Inventory (AC01) — SC-01..SC-10 mapped to Actor, UC, BR, FAC, Proposed Route, API Needs, UI States, Open Decisions

| SC | Name | Actor | UC | BR | FAC | Proposed Route | API Needs (Proposed) | UI States | Open Decisions Impact |
|----|------|-------|----|----|-----|----------------|---------------------|-----------|----------------------|
| SC-01 | الكتالوج العام | الجميع (Visitor, Customer, Employee, Manager) | UC-02 | BR-03, BR-04 | FAC-02 | `/catalog` (public) | GET /developers, /projects, /units with paging/filter/sort | loading, empty, error, success | FE-A002 (RTL), FE-A004 (single app), Q04 (freshness/auto-hide Proposed) |
| SC-02 | تفاصيل الوحدة | الجميع | UC-02, UC-07 | BR-03, BR-04, BR-10, BR-16 | FAC-02, FAC-07 | `/catalog/:unitId` (public) | GET /units/:id, POST /viewing-requests, POST /booking-requests | loading, error, success, forbidden (auth required for actions) | Q04 (price-change hiding Proposed), FE-A005 (local snapshot only) |
| SC-03 | الحساب والجلسة | Customer; Employee details TBD | UC-02 | BR-04 | FAC-02 | `/auth/login`, `/auth/register`, `/auth/recovery` | POST /auth/login, POST /auth/register, POST /auth/refresh, POST /auth/logout | loading, error, success, expired (session) | FE-A001 (layers), FE-A002 (RTL), FE-A003 (four-layer adaptation), Q01 (auth method Proposed) |
| SC-04 | معاينات العميل | صاحب الطلب (Customer) | UC-04, UC-05, UC-06 | BR-07, BR-08, BR-09 | FAC-04, FAC-05, FAC-06 | `/my/viewings` (auth) | GET /viewings/me, POST /viewings, PATCH /viewings/:id/accept, PATCH /viewings/:id/reschedule, DELETE /viewings/:id | loading, empty, error, success, conflict (Q03), stale | Q03 (duration/window/timeout/conflict Proposed), Q07 (prev employee access Proposed), BR-08 (reschedule cancels old immediately) |
| SC-05 | متابعة معاينات الموظف | الموظف المسؤول، المدير | UC-04, UC-05, UC-06 | BR-07, BR-08, BR-09 | FAC-04, FAC-05, FAC-06 | `/employee/viewings` (auth, role=employee/manager) | GET /viewings/assigned, PATCH /viewings/:id/confirm, POST /viewings/:id/alternative, PATCH /viewings/:id/outcome | loading, empty, error, success, forbidden, conflict | Q03 (conflict policy Proposed: first Confirmed wins), Q07 (prev employee NO commands), ACTORS line 9 (P for assigned/manager) |
| SC-06 | العملاء والإسناد | المدير للإسناد، الموظف للمسندين | UC-03 | BR-05, BR-06, BR-15 | FAC-03 | `/admin/clients` (auth, role=manager), `/employee/clients` (auth, role=employee) | GET /clients/unassigned, POST /clients/:id/assign, GET /clients/:id/history, POST /clients/:id/transfer | loading, empty, error, success, forbidden | Q07 (historical access Option A/B Proposed), ACTORS line 11 (manager only assign/transfer), BR-15 (audit log) |
| SC-07 | إدارة المعروض والاتفاقات | الإدارة حسب المصفوفة | UC-01 | BR-01, BR-02, BR-03, BR-14, BR-15 | FAC-01 | `/admin/developers` (auth, role per matrix) | CRUD /developers, /projects, /units, /agreements; PATCH /units/:id/publish, PATCH /units/:id/availability | loading, empty, error, success, forbidden, conflict (duplicate code) | FE-A001 (layers), ACTORS line 15 (manager P, Admin A), Q02 (commission formula Open), Q04 (price-change hiding Proposed) |
| SC-08 | الحجوزات | العميل يرى طلباته؛ الموظف يسجل رد المطور؛ المدير | UC-07, UC-08 | BR-10, BR-11, BR-12, BR-13, BR-16 | FAC-07, FAC-08 | `/my/bookings` (auth, customer), `/employee/bookings` (auth, role=employee), `/admin/bookings` (auth, role=manager) | GET /bookings/me, GET /bookings/assigned, POST /bookings, PATCH /bookings/:id/developer-response, PATCH /bookings/:id/cancel | loading, empty, error, success, forbidden, expired (Q06), conflict (idempotency) | Q06 (cancellation/deposit Proposed), Q03 (response-wait timeout Proposed), BR-12 (expired never auto-Available), BR-13 (deposit separate), FE-A005 (mock not integration) |
| SC-09 | الصفقات والعمولات | مدير المبيعات | UC-09 | BR-13, BR-14, BR-15 | FAC-09 | `/admin/deals` (auth, role=manager) | GET /deals, POST /deals/adopt, GET /deals/:id/commission-snapshot, PATCH /deals/:id/collection | loading, empty, error, success, forbidden | Q02 (commission formula Open blocks final design), Q06 (deposit prerequisite Proposed), BR-15 (snapshot locked), ACTORS line 16 (employee P, manager A) |
| SC-10 | الإشعارات وسجل الحالة | المستلم المسموح | UC-10 | BR-08, BR-15 | FAC-10 | `/notifications` (auth) | GET /notifications/me, GET /notifications/:id/link (with ownership check) | loading, empty, error, success, forbidden (ownership) | BR-08 (reschedule cancels old), BR-15 (audit), ACTORS Q07 (transfer ownership), channel/update method Open |
| **SC-11 (Proposed — GAP)** | **إدارة حسابات الموظفين والصلاحيات** | **System Admin (A), Sales Manager (P)** | **— (No UC assigned yet; gap)** | **BR-01, BR-15** | **— (No FAC assigned yet; gap)** | **`/admin/employees` (auth, role=admin/manager)** | **CRUD /employees, PATCH /employees/:id/permissions, GET /employees/:id/audit-log** | **loading, empty, error, success, forbidden** | **ACTORS line 17 (P for Manager, A for Admin); Q07 audit log visibility; BR-15 (audit). *فجوة: لا UC/FAC معتمد يغطي إدارة الموظفين — يحتاج تنسيق مع مصدر Business. SC-11 ليست في TRACEABILITY.json ولا في validator الحالي. قرار مطلوب: شاشة مستقلة أم جزء من SC-07؟*** |

### Route Map (AC02) — Proposed Angular Routes with Guards/Resolver Notes

| Route | Screen | Guards (Proposed) | Resolver Notes | Layout |
|-------|--------|-------------------|----------------|--------|
| `/` | Redirect to `/catalog` | — | — | Public |
| `/catalog` | SC-01 | — | Units list with filters | PublicLayout |
| `/catalog/:unitId` | SC-02 | — | Unit details + availability | PublicLayout |
| `/auth/login` | SC-03 | GuestOnly | — | AuthLayout |
| `/auth/register` | SC-03 | GuestOnly | — | AuthLayout |
| `/auth/recovery` | SC-03 | GuestOnly | — | AuthLayout |
| `/my/viewings` | SC-04 | AuthGuard (Customer) | MyViewingsResolver | PrivateLayout |
| `/employee/viewings` | SC-05 | AuthGuard (Employee/Manager) | AssignedViewingsResolver | PrivateLayout |
| `/admin/clients` | SC-06 (Manager) | AuthGuard (Manager) | UnassignedClientsResolver | AdminLayout |
| `/employee/clients` | SC-06 (Employee) | AuthGuard (Employee) | AssignedClientsResolver | PrivateLayout |
| `/admin/developers` | SC-07 | AuthGuard (per ACTORS matrix) | DevelopersResolver | AdminLayout |
| `/my/bookings` | SC-08 (Customer) | AuthGuard (Customer) | MyBookingsResolver | PrivateLayout |
| `/employee/bookings` | SC-08 (Employee) | AuthGuard (Employee) | AssignedBookingsResolver | PrivateLayout |
| `/admin/bookings` | SC-08 (Manager) | AuthGuard (Manager) | AllBookingsResolver | AdminLayout |
| `/admin/deals` | SC-09 | AuthGuard (Manager) | DealsResolver | AdminLayout |
| `/notifications` | SC-10 | AuthGuard | NotificationsResolver | PrivateLayout |
| `/admin/employees` | SC-11 (Proposed — GAP) | AuthGuard (Admin/Manager **— Manager remains Proposed per ACTORS line 17**) | EmployeesResolver | AdminLayout |

**Total routes:** 15 routes for SC-01..SC-10 + 1 route for SC-11 (Proposed GAP) + 1 redirect (`/`) = **17 rows**. SC-11 excluded from validator (TRACEABILITY.json covers SC-01..SC-10 only).

Guards are Proposed pending Q01 transport — server owns authorization. Route guards ≠ backend authorization. FE-A001 concerns layer/type organization, not guards. **Sales Manager permission for `/admin/employees` stays Proposed (ACTORS line 17: P) regardless of screen form decision.**
Layouts: PublicLayout (header/footer, no sidebar), PrivateLayout (sidebar for customer/employee), AdminLayout (full admin sidebar). FE-A004 (single app) implies shared routing module.

### Open Decisions Impact on Design (AC03) — Recorded as Proposed/Open, Not Adopted

| Decision | Source | Screens Affected | Design Impact (Potential) |
|----------|--------|------------------|---------------------------|
| FE-A001: تنظيم طبقات/أنواع للواجهة؛ تفاصيله في ARCHITECTURE، بدون Vertical Slice | DECISIONS.md | All | Folder structure, barrel exports, no Vertical Slice |
| FE-A002: العربية وRTL اتجاه مبدئي للمراجعة، وليس قرارًا نهائيًا عن اللغات | DECISIONS.md | All | RTL CSS, flex direction, i18n keys |
| FE-A003: الأربع طبقات تكييف لنفس النظام؛ ليست اقتباسًا حرفيًا لتعريفات المصدر | DECISIONS.md | All | تنظيم فحوص وأدلة التحقق (L1–L4 per VALIDATION.md) — لا service/adapter naming |
| FE-A004: واجهات العميل والموظف والمدير داخل تطبيق واحد مبدئيًا؛ الفصل النهائي في التصميم | DECISIONS.md | All | Route guards, lazy-loaded feature modules per role |
| FE-A005: نسخة قراءة محلية للقواعد مع تحقق مزامنة آلي؛ SC/FAC مراجع تصميم لا صفحات أو اختبارات منفذة | DECISIONS.md | All | SOURCES.json validator, no backend code in frontend |
| Q01: Auth method (email/password, OTP, social?) | MVP_SCOPE.md | SC-03 | Login form fields, session storage, refresh flow |
| Q02: Commission formula details | MVP_SCOPE.md, P01-S03 | SC-07, SC-09 | Commission snapshot fields, calculator UI blocked |
| Q03: Viewing duration, max date, confirmation timeout, conflict prevention | MVP_SCOPE.md, P01-S03 | SC-04, SC-05 | Calendar limits, timer UI, conflict toast, expiry handling |
| Q04: Availability data age, auto-hide on price change | MVP_SCOPE.md, P01-S03 | SC-01, SC-02, SC-07 | Staleness badge, auto-hide toggle (Proposed), price-change banner |
| Q05: Phone customer without account — independent Customer record, link after verification | MVP_SCOPE.md | SC-03, SC-06 | Customer registration flow, phone verification, account linking UI |
| Q06: Deposit/cancellation with external dev coordination | MVP_SCOPE.md, P01-S03 | SC-08, SC-09 | Pending cancellation UI, deposit recording, sale adoption guard |
| Q07: Employee access after transfer (Option A/B) | ACTORS_AND_PERMISSIONS.md, P01-S03 | SC-06, SC-04, SC-05 | Historical data visibility, command disabling, audit log view |

### Traceability Verification (AC04)
- TRACEABILITY.json entries all resolve: 10 UCs, each with rules/screens/acceptance/phases.
- Validator linkage PASS: 16 BR, 10 UC, 10 SC, 10 FAC — all IDs mapped, no duplicates, no unmapped.
- Phase mapping consistent: F03..F08 as per TRACEABILITY.json.
- No drift in IDs. Implementation status all NotStarted. testEvidence all empty.

### Document Check (AC05)
- No Angular build, unit tests, browser tests, or backend integration performed.
- All deliverables are documentation/design artifacts only.

### Self-Review (Reviewer: assistant, Type: Self-review)
- SA01: Screen inventory table complete for SC-01..SC-10; **SC-11 recorded as separate admin screen (design decision)** — no UC/FAC assigned, not in TRACEABILITY.json, validator excludes it (gap documented).
- SA02: Route map with guards/resolver/layout notes for **15 routes (SC-01..SC-10) + 1 SC-11 + 1 redirect = 17 rows**; SC-11 guard marked Proposed (Manager = P per ACTORS line 17). **Guards are Proposed (Q01) — server owns authorization; FE-A001 is folder structure, not guard-related.**
- SA03: **12 open decisions** recorded with explicit per-screen design impact; none converted to Accepted. FE-A001..FE-A005 descriptions match DECISIONS.md exactly.
- SA04: Traceability verified against TRACEABILITY.json; validator linkage PASS for SC-01..SC-10. SC-11 excluded from validator (gap documented).
- **User approval received**: F01-S01 Done. **Design decision**: SC-11 employee accounts = separate screen in admin panel; Sales Manager permission stays Proposed (ACTORS line 17: P). **UC/FAC gap remains open** — requires coordination with Business source before adoption/implementation.
- Status: Done (step complete).

### Limits
- Angular build / unit / browser / backend-integration: Not run — N/A (design step only).
- Backend files: untouched. Proposed/Open decisions remain Proposed/Open.
