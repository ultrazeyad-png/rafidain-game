# رماد الرافدين

لعبة سولز (بأسلوب Dark Souls / Elden Ring / Bloodborne) تمشي بيها من زاخو لبغداد، محافظة ورا محافظة.
البطل **فرات**، يدوّر على مرته **دجلة**، والحقيقة تنكشف بالطريق لحد ما يواجه **أدابا** تحت باب الطلسم ببغداد.

تلعبها هنا: **https://nextlevelnajaf.com/game/**

## شلون تبنيها

ما تحتاج أي مكتبة. ثري دي مكتوب يدوياً بـ WebGL، والأصوات والموسيقى تتولد بالكود.

```
python3 build.py               # يطلع dist/index.html (صفحة وحدة كاملة، تفتحها بأي متصفح)
python3 tools/site_split.py 6  # يطلع site/index.html + site/game.js للموقع (الرقم يكسر الكاش)
```

وللموقع: انسخ `site/index.html` و `site/game.js` لـ `nextlevel-v3/frontend/public/game/` وانشر.

## الملفات

| الملف | شنو بيه |
|---|---|
| `src/template.html` | الصفحة: الأزرار، الواجهة، الحركة، القتال، اللاعب، الأعداء، الخريطة، الحفظ |
| `src/gl.js` | الرسم بـ WebGL: الشيدر، الظلال، السما، الماي، تجميع العالم الثابت |
| `src/regions.js` | زاخو ودهوك وأوروك (مبنيات باليد) وتحميل المناطق |
| `src/gen.js` | باقي المحافظات من وصف لكل وحدة، البوسات البشرية، خريطة العراق، القارب والنهاية |
| `src/depth.js` | الأسرار، رسائل دجلة، الكلمات الأخيرة، الألواح، الدروع، الوحوش الجديدة، الموسيقى، الطقس |
| `src/bosses.js` | محرك البوسات (نمتار وإرّا والباقين)، المشاهد السينمائية للبوسات |
| `src/systems.js` | الحچي والدفتر، المشاعل، أنزو (قديم)، المقدمة السينمائية |
| `src/chars.js` | رسم البطل والأعداء والشخصيات وحركات السيف |
| `src/audio.js` | كل الأصوات وطبول البوس |
| `src/logo.b64` | شعار Next Level |
| `tests/` | فحوصات بالمتصفح الآلي (Playwright + Chrome) |

## الفحوصات

تحتاج Python و Playwright و Google Chrome:

```
python3 build.py
python3 tests/d4.py   # طريق زاخو كامل: الصحوة، الجسر، القطع، المشاعل، إرّا، السفر لأوروك
python3 tests/g1.py nineveh,anbar,maysan,baghdad   # يمشي بالمحافظات
python3 tests/g7.py   # الأسرار والرسائل والألواح والسحر
```

## فحوصات الجودة الجديدة

للفحص السريع القابل للتشغيل في CI:

```bash
python -m py_compile build.py tools/site_split.py tests/*.py
python tests/smoke_build.py
```

لاختبار استعادة WebGL في Chromium (يتطلب Node.js وPlaywright):

```bash
python -m pip install -r requirements-test.txt
python -m playwright install chromium
python tests/test_webgl_recovery.py
```

تعمل فحوصات الجودة تلقائياً عند فتح Pull Request أو الدفع إلى فروع التطوير. لا تدمج التغييرات قبل نجاحها.

## الخطوة الجاية

تحويلها لتطبيق APK للأندرويد.
