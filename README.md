# BrokerHub — نظام إدارة الشغل

هذا المجلد يحتوي Workflow ووثائق فقط، وليس تطبيقًا.
اسم BrokerHub مؤقت. المشروع لشركة بروكر تسوق وحدات لعدة مطورين.

ابدأ من docs/WORKFLOW.md وdocs/CURRENT_STATE.md.
لفهم المشروع وطريقة التنفيذ معًا: [Business + Workflow](docs/business/README.md).
بدأت مرحلة Backend P01؛ أول مخرج للمراجعة: docs/MVP_SCOPE.md.
السياق: docs/PROJECT_CONTEXT.md. القرارات: docs/DECISIONS.md.
التسليم: handoffs/CURRENT_HANDOFF.md.

## التنظيم
- docs/: متطلبات، قرارات، مخاطر، معمارية مقترحة، وحالة.
- planning/: مراحل وخطوات وأدلة ومراجعات.
- planning/templates/: قوالب قابلة لإعادة الاستخدام.
- handoffs/: التسليم بين الجلسات والموديلات.
- scripts/: فحص سلامة ملفات العمل.
- AGENTS.md: قواعد أي مساعد يعمل على المشروع.

## التحقق
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate.ps1

هذا فحص وثائق، وليس build أو اختبارات تطبيق.
لبدء جلسة جديدة استخدم planning/SESSION_START.md.
