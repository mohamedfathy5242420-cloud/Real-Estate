# P02 — أساس الحل البرمجي (Solution Foundation)

Status: InProgress
بدأت بعد إغلاق P01 بمراجعة ذاتية معتمدة من المستخدم. P01-S04 أنتجت نموذج البيانات، ERD، القيود، وخطة الأساس التقني (P02_TECHNICAL_FOUNDATION.md).

## خطوات P02

| الخطوة | الوصف | الحالة |
| --- | --- | --- |
| S01 | Solution ومشاريع الطبقات وقوالب اختبار فارغة وhealth وإعدادات البناء | Done |
| S02 | حدود هوية وصلاحيات مستقلة عن وسيلة تسجيل الدخول | Done |
| S03 | SQL Server Identity persistence وCookie/current-user plumbing | InReview |
| S04 | Account HTTP journeys: register/login/logout/confirm/recovery وstaff provisioning | Pending |

الكتالوج والاتفاقات يبدأان في P03، والعملاء في P04، والمعاينات في P05، والحجوزات في P06،
والصفقات في P07، والبنية غير المتزامنة في P08، وتكامل Angular في P09.

## القواعد
- Clean Architecture حسب الطبقة/النوع؛ لا Vertical Slice (D002).
- تطبيق موضوعات الكورس تدريجيًا مع فهم ومراجعة.
- لا نشر، لا Docker، لا مفاتيح حقيقية في هذا المشروع.
- Q01/Q05 حُسمتا في D033–D035، وSQL Server في D036. Q04/Q06/Q07 والسياسات
  المفتوحة تُحسم قبل خطواتها المتأثرة.

## التحقق
- `scripts/validate.ps1` يفحص وجود ملفات تخطيط P02 والاتساق.
- أوامر restore/build وفحص health تسجل في evidence-S01.md؛ الاختبارات الآلية مؤجلة بـD031.
