# مساهمة وتطوير

## قواعد المستودع

- استخدم فروعًا قصيرة باسم واضح مثل `feature/customer-search` أو `fix/payment-rounding`.
- اجعل كل تغيير مركزًا، واكتب رسالة commit تصف التغيير بوضوح.
- لا ترفع بيانات حقيقية أو أسرار اتصال أو ملفات نسخ احتياطي. افحص `git status` قبل commit.
- وثّق أي تغيير في قواعد الفوترة أو الاستهلاك في `docs/` بالعربية والفرنسية.
- حافظ على ترجمة واجهة المستخدم باللغتين وعلى اتجاه RTL/LTR.

## قبل إرسال التغييرات

- اشرح السلوك المتأثر وأي قرار مالي أو تشغيلي.
- حدّث الوثائق ونموذج الإعدادات الآمنة عند الحاجة.
- لا تضف بيانات اعتماد أو معلومات زبائن إلى الأمثلة أو لقطات الشاشة.

## التحقق محلياً

يتطلب البناء SDK .NET 10. من جذر المستودع شغّل:

```powershell
dotnet restore Hawdh.Platform.slnx
dotnet build Hawdh.Platform.slnx --configuration Release --no-restore
dotnet test Hawdh.Platform.slnx --configuration Release --no-build
```

يتحقق GitHub Actions من استعادة الحزم وتنبيهات NuGet، وصياغة سكربتات النسخ والاستعادة، وملف Compose الإنتاجي، والبناء والاختبارات، وإنشاء صورة Docker. يتطلب تحقق Compose وصورة Docker تثبيت Docker مع Compose plugin.
