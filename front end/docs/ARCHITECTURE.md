# Angular frontend architecture

F01-S04 وضع الخطة، وF02-S01 نفّذ أساس Angular داخل `app/`. الموجود حاليًا shell
وroute placeholders وحدود hybrid rendering فقط؛ ميزات الـBusiness والـAPI الحي غير منفذة.

تنظيم حسب النوع والمسؤولية:
- pages/: صفحات وتوجيه الرحلات.
- components/: مكونات عرض قابلة لإعادة الاستخدام.
- layouts/: هياكل صفحات العميل ولوحة الموظفين.
- services/: حالات الاستخدام وتنسيق الحالة.
- api/: adapters وعقود HTTP وتحويل البيانات.
- models/: نماذج العرض والأنواع.
- guards/ وinterceptors/: تجربة التنقل ومعالجة الطلبات المشتركة.
- styles/: أنماط وهوية واتجاه العرض.

لا اتصال HTTP مباشر داخل مكونات العرض. الخادم يحسم السعر والتوفر والصلاحيات.
نفصل نماذج العميل عن تفاصيل الموظفين والملاحظات الداخلية.
المسارات في F01-S01 مقترحة، وواجهة العقد في API_CONTRACT ما زالت Proposed؛ لا توجد أسماء endpoints معتمدة بعد.

قرارات تصميم مطلوبة: بيئة استضافة SSR؛ سياسة الجلسة مع Backend؛ مكتبة UI إن لزمت؛
العربية/RTL مستقبلًا، العملة والتوقيت والعقود الفعلية. تراجع إصدارات الأدوات وتوافقها
من المصادر الرسمية وقت بدء التنفيذ.

## اتجاه التصيير للفهرسة — Implemented foundation for FE-D009

FE-D009 يحسم حاجة المنتج: فهرسة الكتالوج وتفاصيل الوحدات العامة في Google من أول إصدار.
الأساس المنفذ يستخدم Hybrid Angular rendering: `RenderMode.Server` لمساري `/catalog` و`/catalog/:unitId`
لأن السعر والتوفر يتغيران، و`RenderMode.Client` للمسارات غير المفهرسة: الدخول/التسجيل
العامة SC-03 وصفحات العميل والموظف والمدير والإدارة SC-04..SC-10. Prerender لصفحات
عامة ثابتة فقط إن ظهرت حاجة لها.
عند إعداد SSR تحدد server routes صراحة؛ لا نفترض أن إعداد prerender الافتراضي مناسب
للمخزون المتغير. الاستضافة تحتاج runtime قادرًا على الرد على طلبات SSR، وسياسة cache
وproxy واتصال API عام مجهول آمن. المزود والبيئة لم يحددا بعد. إعداد التطوير يسمح
`localhost` و`127.0.0.1` فقط؛ بيئة النشر تضبط `NG_ALLOWED_HOSTS` للدومينات الفعلية.

HTML العام وبيانات hydration/transfer لا تحمل جلسة أو ملاحظات أو بيانات موظف. القراءة
العامة للـSSR تستخدم إسقاط الكتالوج المنشور فقط، ولا تعرض وحدة مخفية. الصفحة العامة
تحتاج URL ثابتًا وmetadata وروابط قابلة للزحف؛ تصفية/ترتيب URLs وcanonical/sitemap
تراجع في F02. يمكن اختبار HTML الأولي والفهرسة التقنية، لكن الإدراج الفعلي في Google
قرار محرك البحث وليس نتيجة مضمونة من SSR وحده.

المصادر الرسمية: https://angular.dev/guide/routing/rendering-strategies ؛
https://angular.dev/guide/ssr ؛
https://developers.google.com/search/docs/crawling-indexing/javascript/javascript-seo-basics .

## أساس أدوات F02 — منفذ في F02-S01

أعيد الفحص في 2026-09-24: Angular 22 ما زال Active، وتم إنشاء التطبيق بـAngular CLI
22.1.8 على Node 24.19.0 وpnpm 11.19.0، مع standalone وrouting وstrict وSSR per route
وVitest. الإصدارات الدقيقة مقفولة في `app/pnpm-lock.yaml`.
المراجع: https://angular.dev/reference/releases ؛ https://angular.dev/reference/versions ؛
https://nodejs.org/en/about/previous-releases ؛ https://angular.dev/cli/new .

نظام الملفات يتبع قرار المشروع layer/type بلا Vertical Slice: صفحات في `pages/`،
مكونات عرض في `components/`، خدمة تنسق حالة الاستخدام في `services/`، نقل HTTP وتحويل DTO
في `api/`، نماذج عرض في `models/`، layouts وguards وstyles منفصلة. الشاشات يمكن
تحميلها lazy على حدود route مع بقاء تنظيم الطبقات. هذا قيد المشروع المقصود حتى لو
كان دليل Angular العام يفضل ترتيبًا مختلفًا؛ لا يغيّر الدليل قرار المستخدم.

Typed reactive forms مرشحة للنماذج المعقدة؛ Signals للحالة المحلية للشاشة،
وتأجيل مكتبة global state إلى حاجة مثبتة. `HttpClient` adapters وfunctional interceptors
تأتي بعد عقد API والجلسة. Route guards تنظم تجربة التنقل فقط؛ الخادم يتحقق من الدور
والملكية والإسناد لكل قراءة وأمر. مصدر Angular: https://angular.dev/guide/forms/typed-forms ؛
https://angular.dev/guide/signals ؛ https://angular.dev/guide/http/setup ؛
https://angular.dev/guide/routing/route-guards .

## تقسيم المسؤولية والتحقق

| Backend | Frontend |
| --- | --- |
| يحدد العقد الفعلي ويفرض المصادقة والملكية والإسناد والانتقالات والوقت والتعارض، ويحفظ التواريخ واللقطات والإخطارات | يرسل المدخلات، يتحقق مبكرًا لأجل UX، ويعرض نتيجة الخادم والحالات loading/empty/error/forbidden/stale دون نجاح متفائل كاذب |
| يرجع إسقاطات عامة/عميل/موظف منفصلة؛ لا ملاحظات داخلية في رد العميل | لا يضع حقول موظف في نموذج العميل أو HTML العام؛ يفحص العرض ولوحة المفاتيح والتجاوب |
| يثبت مرجع المطور وسعر الحجز والعمولة المحفوظة ويعيد أخطاء typed | يعرض السعر/المهلة/snapshot المرسلة ولا يعيد احتساب قرار Business من cache |

فحوص F02 المقترحة: build/type-check وunit للنماذج والخدمات، اختبارات adapters
على contract fixtures متفق عليها، browser journeys لحالات FAC، 320px ولوحة المفاتيح،
وفحص SSR HTML العام/metadata ومنع تسرب البيانات. تكامل Backend لا يسمى ناجحًا
إلا عند اختبار API فعلي. Vitest هو ترشيح أداة الاختبار الافتراضية من Angular CLI
وقت إنشاء التطبيق، ويعاد التحقق منه حينها. لا واحد من هذه الفحوص شُغل الآن.

قبل خطوة F02 التي تبني الجلسة يجب مواءمة FE-D008 مع Q01 في مصدر Business وحسم
cookie/token وCSRF والتحقق والاستعادة ودخول الموظفين. يمكن تجهيز shell عام في خطوة
محدودة قبل اكتمال API، لكن تنفيذ auth/viewing/booking يعتمد العقد الفعلي. قبل SSR
يلزم اختيار استضافة تدعمه وقراءة عامة آمنة للكتالوج؛ قبل SC-11 يلزم UC/FAC أساسي.
