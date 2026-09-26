# BrokerHub — Angular frontend

فولدر مستقل باسم `front end`. وثائق التخطيط والمراجعة موجودة في الجذر، وأساس تطبيق
Angular موجود في `app/`. التطبيق حاليًا foundation فقط: المسارات صفحات placeholder
صادقة، من غير Auth أو API حي أو تنفيذ لشاشات الـBusiness.

ابدأ من [طريقة العمل](docs/WORKFLOW.md)، ثم [الحالة الحالية](docs/CURRENT_STATE.md).
لفهم المنتج وربطه بالشاشات والخطوات: [Business + Frontend Workflow](docs/business/README.md).
راجع [المراحل](docs/ROADMAP.md)، [التصميم المقترح](docs/ARCHITECTURE.md)
و[عقد التكامل مع Backend](docs/API_CONTRACT.md).

## المكونات
- docs/: السياق والقرارات والمخاطر والتحقق والمعمارية.
- planning/: المراحل والخطوات والقوالب والأدلة والمراجعات.
- handoffs/: تسليم مستقل للواجهة.
- app/: Angular 22 standalone app مع routing وhybrid SSR/CSR وVitest.
- scripts/validate.ps1: فحص سلامة ملفات الـWorkflow.
- AGENTS.md: تعليمات المساعد داخل هذا الفولدر.

## التحقق من داخل هذا الفولدر
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate.ps1

أو من D:/Practice:
powershell -NoProfile -ExecutionPolicy Bypass -File "front end/scripts/validate.ps1"

لبدء جلسة جديدة: planning/SESSION_START.md.
المسارات تعمل مع المسافة في اسم الفولدر. لا يعتمد السكريبت على ملفات Backend.
