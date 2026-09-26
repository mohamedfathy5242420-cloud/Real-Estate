# Business + Workflow: نقطة البداية

هذا الفولدر هو المرجع التفصيلي المشترك للـBusiness. docs/PROJECT_CONTEXT.md ملخص،
docs/MVP_SCOPE.md حدود النسخة والأسئلة Q01–Q07، وdocs/DECISIONS.md سجل مصدر القرارات.
لا تعتمد على ملخص المحادثة أو على نسخة Frontend قديمة بدل هذا المرجع.

## ترتيب القراءة لأي Agent
1. AGENTS.md ثم docs/CURRENT_STATE.md وhandoffs/CURRENT_HANDOFF.md.
2. BUSINESS_OVERVIEW.md وACTORS_AND_PERMISSIONS.md.
3. BUSINESS_RULES.md ثم USE_CASES.md وSTATE_TRANSITIONS.md.
4. CONCURRENCY_AND_IDEMPOTENCY.md ثم DECISION_PACKAGE_P01_S03.md؛ كلاهما تصميم
   ومقترحات، ولا يحول وجودهما أي قرار إلى Accepted.
5. DOMAIN_MODEL.md ثم ERD.md وCONSTRAINTS.md لفهم نموذج البيانات وحدود القرارات.
6. P02_TECHNICAL_FOUNDATION.md لخطة الأساس البرمجي، وليس كدليل أن الكود موجود.
7. ACCEPTANCE_SCENARIOS.md وTRACEABILITY.json.
8. docs/WORKFLOW.md وdocs/VALIDATION.md والخطوة النشطة.

## كيف يعمل الربط؟
قرار المستخدم → BR (قاعدة) → UC (حالة استخدام) → AC (سيناريو قبول) → مرحلة تنفيذ → اختبار ودليل.
TRACEABILITY.json خريطة قابلة للفحص، وليست دليلًا أن الكود أو الاختبار موجود.
كل خطوة قادمة تحدد BusinessRefs وUseCaseRefs وAcceptanceRefs والأجزاء التي ستنفذها بالضبط.

مثال: D007 → BR-08 → UC-05 → AC-07 → P05.
المطلوب: طلب إعادة الجدولة يلغي الموعد القديم، ويعيد انتظار التأكيد.
اختبار لاحق يتحقق من عدم بقاء الموعد القديم مؤكدًا، وغياب تأكيد تلقائي للجديد.

## الثقة والقرارات
Accepted: قرار صريح أو من المسودة التي وافق المستخدم عليها للانطلاق.
Proposed: تمثيل أو اختيار تفصيلي لم يعتمد بعد؛ لا تنفذه كقاعدة نهائية.
Open: نقطة تحتاج حسمًا قبل تصميم أو تنفيذ الجزء المتأثر.
أسماء enums وتفاصيل الصلاحيات الجديدة مقترحات حتى لو كانت القاعدة العامة Accepted.

## تغيير قاعدة
سجل مصدر القرار وحالته في DECISIONS، وعدل القاعدة والرحلة والسيناريو والخريطة.
حدد أثره على STATE_TRANSITIONS وعقود Angular، ثم حدث الحالة والتسليم.
لا تغيّر Accepted لتناسب التنفيذ. إذا تعارض مصدران وثق التعارض ولا تخمّن.
لا تكرر نصوص Business كاملة داخل الخطوات؛ استخدم IDs وروابط الملفات.

## الحالي
تم توثيق Business وربطه بالـWorkflow؛ لا يوجد تطبيق أو اختبارات Business.
حالة Q01–Q07 مسجلة في MVP_SCOPE وDECISIONS؛ Q01/Q02/Q03/Q05 Accepted والباقي لا يتحول
إلى موافقة لمجرد ظهوره في نموذج أو ERD.
