# BrokerHub Angular frontend — working contract
This directory is the frontend workflow root. Resolve paths here, not against D:/Practice.
Current authorization: بدء شغل الـFrontend تدريجيًا حسب الـWorkflow (يشمل تنفيذ Angular بعد متطلبات التصميم والقرارات اللازمة له)؛ ليس تصريحًا بتجاوز المراحل أو اعتبار المقترحات موافقات.
Rule: بعد كل خطوة سلّم Handoff كاملًا ثم توقف واستنى تصريحًا صريحًا للخطوة التالية — لا تنتقل تلقائيًا.
Read docs/PROJECT_CONTEXT.md, docs/DECISIONS.md, docs/RISKS.md,
docs/CURRENT_STATE.md, docs/WORKFLOW.md, docs/VALIDATION.md,
docs/API_CONTRACT.md, handoffs/CURRENT_HANDOFF.md and the active step.
Also read docs/business/README.md and its full reading order: shared business, screens, frontend acceptance and traceability.

Follow the parent working principles, with frontend-specific boundaries:
- Angular is approved. Version, UI library, auth transport and rendering mode are undecided.
- Layer/type separation: pages/components, application services, API adapters and models.
  Do not copy .NET project structure or introduce Vertical Slice.
- One bounded active step. Record real evidence and label self-review.
- Roles are planner/implementer/reviewer, not fixed model names. No automatic agents.
- Preserve backend files. Business changes require documented synchronization.
- docs/business/shared is a read-only business snapshot; docs/business/SOURCES.json records its canonical sources. Validate synchronization before work when the backend source is available.
- If the backend source is absent, explicitly report synchronization as Not verified, not passed. Local snapshot supports reading but is not proof of latest decisions.
- المصدر الأساسي D:/Practice/docs/business للقراءة فقط (مقارنة/مزامنة نسخة Frontend)؛ ممنوع تعديله أو قراءة كود Backend أو أي مسارات خارج front end. بعد اكتمال المزامنة اعتمد النسخة المحلية، وأعد فحص المزامنة فقط عند بداية مرحلة جديدة أو ظهور دليل تغيير أو طلب صريح.
- Each step must list BusinessRefs, UseCaseRefs, ScreenRefs and AcceptanceRefs; map BR/UC to SC/FAC and implementation phases.
- Proposed UI and open business decisions are not approval. NotStarted trace entries do not mean code exists.
- Never invent an implemented API from a proposed contract or mock.
- Client route guards do not replace backend authorization.
- Never expose internal sales notes in customer payloads, state or views.
- Scope includes loading/empty/error/forbidden/stale states, keyboard access and responsive layouts.
- Do not claim browser, unit or integration tests passed before those checks exist and run.
- Update local state, handoff, evidence and review. Roadmap is not implementation permission.
- Communicate in Egyptian Arabic and keep steps suitable for learning and review.
