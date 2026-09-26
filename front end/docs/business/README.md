# Angular Business + Workflow

ابدأ هنا بعد AGENTS والحالة والتسليم. هذه نقطة الدخول لفهم Business وكيف يظهر في Angular.
لا يوجد تطبيق أو عقد HTTP منفذ في هذه المرحلة.

## اقرأ بالترتيب
1. shared/BUSINESS_OVERVIEW.md: الفكرة والمصطلحات.
2. shared/ACTORS_AND_PERMISSIONS.md وshared/BUSINESS_RULES.md: الأدوار وقواعد BR.
3. shared/USE_CASES.md وshared/STATE_TRANSITIONS.md: الرحلات والحالات.
4. shared/MVP_SCOPE.md: النطاق والأسئلة المفتوحة Q01–Q07.
5. SCREENS_AND_FLOWS.md: ربط رحلات UC بالشاشات SC.
6. UI_ACCEPTANCE.md: سيناريوهات FAC للواجهة؛ shared/ACCEPTANCE_SCENARIOS.md قبول Business العام.
7. TRACEABILITY.json ثم ../WORKFLOW.md والخطوة النشطة.

## مصدر واحد ونسخة مستقلة
قواعد Business الأصلية في D:/Practice/docs/business. shared نسخة قراءة محلية تسمح
للـAgent بفهم المشروع حتى لو فتح front end وحده. لا تعدل قواعد النسخة منفردة.
SOURCES.json يحدد مصدر كل نسخة. السكريبت يقارن المصدر والنسخة عند وجود المجلد الأب.
عند غياب المصدر يعلن عدم التحقق من المزامنة، ويستمر فحص المراجع المحلية؛ لا يدعي أنها أحدث نسخة.
عند تغير المصدر يفشل التحقق حتى تراجع الفرق وتنقل التغيير وتحدث أثره على الشاشات والاختبارات والعقد.
النسخ تحفظ Accepted/Proposed/Open كما هي. مراحل P في ملفات shared تخص Backend، ومراحل F في TRACEABILITY تخص Frontend.

## الربط المطلوب في كل خطوة
BR → UC → SC → FAC → F phase → test evidence.
BusinessRefs وUseCaseRefs وScreenRefs وAcceptanceRefs حقول إلزامية،
أو N/A مع سبب للخطوة التقنية البحتة.
مثال BR-08 → UC-05 → SC-04/SC-05 → FAC-05 → F04.
إعادة الجدولة لا تظهر الموعد الجديد مؤكدًا؛ الإلغاء الفوري للقديم يعكس رد الخادم.
الخادم يحسم الوقت والتوفر والصلاحيات. إخفاء زر ليس authorization.

## نقطة الاستئناف
الحالة الحالية في `../CURRENT_STATE.md`؛ F00 مكتملة وF01 قيد التصميم. Q02/Q03 حُسمتا في D015–D030؛ Q01 وQ04–Q07 مفتوحة.
مقترحات شكل الشاشات أو اللغة أو enum ليست قرارات Business جديدة.
