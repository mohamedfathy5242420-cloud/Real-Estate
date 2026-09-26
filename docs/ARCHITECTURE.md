# Architecture — حدود تصميم وليست تنفيذًا

المؤكد: Clean Architecture بتنظيم حسب الطبقة والنوع، بدون Vertical Slice.

المقترح:
Domain ← Application ← Infrastructure.
API يستخدم Application ويربط Infrastructure عند تركيب الخدمات.

Domain: Entities, Enums, ValueObjects.
Application: DTOs, Interfaces, Services, Commands, Queries, Handlers, Validators, Facades, Orchestrators.
Infrastructure: Persistence, Repositories, Messaging, Caching.
API: Controllers, Middleware.

المشاريع موجودة فعليًا في `src/` و`tests/` داخل `BrokerHub.sln`. اتجاه المراجع الحالي:
Domain بلا مراجع داخلية؛ Application → Domain؛ Infrastructure → Application؛
API → Application + Infrastructure. التنظيم حسب الطبقة والنوع، لا Vertical Slice.

## مواضع موضوعات الكورس المقترحة
Repository/Services للبيانات والقواعد؛ DTOs/ViewModels لعقود العرض.
CQRS/Mediator لفصل وتوجيه القراءة والكتابة؛ Orchestrator لرحلة الحجز.
Facade لتجميع عمليات متماسكة عند الحاجة.
SaveChanges/Transactions على مستوى العملية، لا حفظ داخل كل Repository.
RabbitMQ/Outbox لإشعارات موثوقة وإعادة محاولة ومنع تكرار الأثر.
Caching للكتالوج، وليس مصدر تأكيد الحجز.
Channels لمعالجة داخلية غير حرجة، وليس لتخزين الحجز الموثوق.

الواجهة Angular داخل front end؛ لا تنفيذ لها ضمن مهمة Backend.
تفاصيل P02 في docs/business/P02_TECHNICAL_FOUNDATION.md: .NET 10 LTS SDK 10.0.401
و`net10.0` مثبتان حسب D032. توجد إعدادات بناء وحزم مركزية وHealth وDI skeleton.
SQL Server/EF Core وASP.NET Core Identity يبدأان في P02-S03 وفق D033–D036،
والاختبارات الآلية مؤجلة بقرار D031.
