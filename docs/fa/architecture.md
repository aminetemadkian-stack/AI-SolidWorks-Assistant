# 🧠 معماری کامل پروژه — سند مرجع (Project Brain)

> این سند، مرجع کامل و رسمی چشم‌انداز، معماری و نقشه‌راه پروژه است.
> همه‌ی توسعه‌های آینده باید با این سند هماهنگ باشند.
> (تکمیل‌شده‌ی `README_FA.md` و `docs/fa/developer-guide.md`)

---

## ۱. چارت شماتیک کل پروژه

```
                         ┌─────────────────────────────┐
                         │          USER               │
                         │  فارسی / English / Natural  │
                         └──────────────┬──────────────┘
                                        │
                                        ▼
                    ┌────────────────────────────────────┐
                    │      SOLIDWORKS AI ASSISTANT       │
                    │        C# SOLIDWORKS ADD-IN        │
                    └──────────────────┬─────────────────┘
                                       │
                 ┌─────────────────────┼─────────────────────┐
                 │                     │                     │
                 ▼                     ▼                     ▼
        ┌────────────────┐    ┌────────────────┐    ┌────────────────┐
        │   AI CHAT UI   │    │ MODEL CONTEXT  │    │ COMMAND CENTER │
        │ RTL / LTR      │    │ CAD State      │    │ Settings       │
        └───────┬────────┘    └───────┬────────┘    └────────────────┘
                │                     │
                └──────────┬──────────┘
                           ▼
                 ┌──────────────────────┐
                 │      AI CORE         │
                 │                      │
                 │ Intent Detection     │
                 │ Planning             │
                 │ Context              │
                 │ Validation           │
                 │ Safety               │
                 └──────────┬───────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │      AI ROUTER       │
                 │     Online First     │
                 └──────────┬───────────┘
                            │
                  ┌─────────┴─────────┐
                  │                   │
             INTERNET OK          INTERNET OFF
                  │                   │
                  ▼                   ▼
        ┌─────────────────┐   ┌─────────────────┐
        │   CLOUD AI      │   │   LOCAL AI      │
        │                 │   │                 │
        │ GPT             │   │ Ollama          │
        │ Claude          │   │ Qwen            │
        │ Gemini          │   │ DeepSeek        │
        └────────┬────────┘   └────────┬────────┘
                 │                     │
                 └──────────┬──────────┘
                            ▼
                 ┌──────────────────────┐
                 │  CAD INTERPRETER     │
                 │                      │
                 │ Natural Language     │
                 │       ↓              │
                 │ Structured Commands  │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │   COMMAND ENGINE     │
                 ├──────────────────────┤
                 │ Sketch               │
                 │ Extrude              │
                 │ Cut                  │
                 │ Hole                 │
                 │ Fillet               │
                 │ Chamfer              │
                 │ Pattern              │
                 │ Assembly             │
                 │ Drawing              │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │   SOLIDWORKS API     │
                 └──────────┬───────────┘
                            ▼
                 ┌──────────────────────┐
                 │      SOLIDWORKS      │
                 │                      │
                 │ Part                 │
                 │ Assembly             │
                 │ Drawing              │
                 └──────────────────────┘
```

---

## ۲. کار هر بخش

### 2.1 — User Interface
رابطی که کاربر با آن صحبت می‌کند. مثلاً:

> «یک براکت ۱۰۰ در ۵۰ میلی‌متر بساز و دو سوراخ ۱۰ میلی‌متری ایجاد کن.»

پشتیبانی از:
- فارسی
- انگلیسی
- RTL/LTR
- تاریخچه گفتگو
- تأیید اجرای دستورات

### 2.2 — Model Context
وضعیت فعلی SolidWorks را می‌خواند:

- Part فعلی
- Assembly
- Feature Tree
- Sketch
- Dimensions
- Material
- Selected Face/Edge
- Configuration

تا AI بداند **الان دقیقاً چه چیزی جلوی کاربر است.**

### 2.3 — AI Core
مغز اصلی سیستم:

```
درخواست کاربر
↓
درک هدف
↓
برنامه‌ریزی
↓
تبدیل به عملیات CAD
↓
اعتبارسنجی
↓
اجرای امن
```

### 2.4 — AI Router
یکی از مهم‌ترین قسمت‌ها. قانون پروژه:

**Online First → Offline Fallback**

```
Cloud AI
   │
   ├── موفق → استفاده
   │
   └── Fail
        ↓
     Ollama
```

کاربر لازم نیست هر بار دستی تغییر دهد.

### 2.5 — Cloud Providers
در آینده امکان استفاده از چند Provider:

- OpenAI
- Anthropic
- Google

و Router می‌تواند Provider مناسب را انتخاب کند.

### 2.6 — Local AI
برای زمان‌هایی که اینترنت قطع است یا کاربر نمی‌خواهد داده‌ای ارسال شود:

```
Ollama
├── Qwen
├── DeepSeek
└── سایر مدل‌های محلی
```

### 2.7 — CAD Interpreter
تبدیل زبان طبیعی به ساختار دقیق. مثلاً:

> «یه سوراخ ده میلی‌متری وسطش بزن.»

تبدیل می‌شود به:

```
operation = hole
diameter = 10 mm
position = center
```

و **AI مستقیماً اجازه اجرای کد دلخواه روی SolidWorks را ندارد.**

### 2.8 — Command Engine
فرمان‌های استاندارد CAD را اجرا می‌کند. نسخه اول:

```
Sketch
Line
Rectangle
Circle
Dimension

Extrude
Cut
Hole

Fillet
Chamfer

Pattern
Mirror

Assembly
Mate
```

بعداً بسیار گسترده‌تر می‌شود.

### 2.9 — SolidWorks API Layer
تنها لایه‌ای که اجازه دارد با SolidWorks تعامل واقعی داشته باشد.
این جداسازی باعث می‌شود امنیت و تست سیستم بسیار بهتر شود.

---

## ۳. Roadmap پروژه

### Phase 0 — Project Foundation ✅ (انجام شد)
**هدف: ایجاد پایه GitHub**
- Public Repository
- Git structure
- License
- README
- Architecture
- Contribution rules
- Security policy
- ۱۰ زبان مستندات
- Step-by-step installation برای هر ۱۰ زبان

**خروجی:**
```
GitHub Repository
        +
Documentation Foundation
```

### Phase 1 — SolidWorks Add-in 🚧 (در حال ساخت — اسکلت کامل ساخته شد، در انتظار تست روی ماشین واقعی سالیدورک)
**هدف: ورود واقعی به SolidWorks**

ساخت:
```
C# Add-in
       ↓
SolidWorks API
```

قابلیت:
- Load شدن Add-in
- پنل AI
- اتصال به SolidWorks
- خواندن Model
- خواندن Selection

**Milestone:** AI Assistant داخل SolidWorks باز می‌شود.

**وضعیت پیاده‌سازی (این ریپو):**
- ✅ اسکلت کامل C# در `SolidWorks_Addin/` — ورودی COM (`ISwAddin` با GUID راستی‌آزمایی‌شده)، پنل Task Pane با چت RTL فارسی/انگلیسی
- ✅ خواننده‌ی وضعیت مدل (سند فعال، کانفیگ، درخت فیچرها، انتخاب‌ها) با late-binding — بدون نیاز به DLLهای interop در بیلد
- ✅ فرمت PlanScript + پارسر (پشتیبانی اعداد فارسی) + اعتبارسنج ایمن
- ✅ خط‌لوله‌ی تأیید-قبل-از-اجرا + تولید ماکروی VBA برای ۵ عملیات (مستطیل، دایره، اکسترود، برش، سوراخ)
- ✅ ~۳۵ تست واحد + CI ویندوزی (GitHub Actions)
- ⏳ تست نهایی روی ماشین واقعی با سالیدورک نصب‌شده (نیازمند ویندوز + سالیدورک)

### Phase 2 — AI Core
ساخت:
```
AI Controller
AI Router
Context Manager
Command Parser
Validator
```

در این مرحله هنوز هدف اصلی ساخت CAD پیچیده نیست؛ پایه هوش مصنوعی را درست می‌سازیم.

### Phase 3 — Online AI
اتصال:
```
SolidWorks
 ↓
AI Core
 ↓
Cloud Provider
 ↓
Response
```

اولین Provider اضافه می‌شود و معماری برای Providerهای دیگر باز می‌ماند.

### Phase 4 — Offline AI
اتصال:
```
AI Core
 ↓
Ollama
 ↓
Qwen / DeepSeek
```

تست:
```
Internet ON  → Cloud
Internet OFF → Ollama
```

**بدون قطع شدن تجربه کاربر.**

### Phase 5 — Persian Engineering AI
ورود جدی فارسی به سیستم. مثلاً:

```
«یک مستطیل ۲۰۰ در ۱۰۰ بکش»
          ↓
Create Sketch
Rectangle
Width = 200 mm
Height = 100 mm
```

شامل:
- فارسی محاوره‌ای
- فارسی رسمی
- اصطلاحات مهندسی
- واحدها: متر، سانتی‌متر، میلی‌متر، اینچ
- فارسی + English در یک جمله

### Phase 6 — CAD Command Engine
ساخت عملیات واقعی:

**Sketch:**
```
Line
Circle
Rectangle
Arc
Dimension
Constraint
```

**Features:**
```
Extrude
Revolve
Cut
Hole
Fillet
Chamfer
Shell
Pattern
Mirror
```

### Phase 7 — Intelligent Modeling
تبدیل سیستم از «فرمان اجرا کن» به **دستیار طراحی**. مثلاً:

> «این قطعه را ۲۰ درصد سبک‌تر کن ولی ضخامت دیواره کمتر از ۳ میلی‌متر نشود.»

AI:
```
Analyze Model
       ↓
Find Candidates
       ↓
Generate Plan
       ↓
Show Proposed Changes
       ↓
User Approval
       ↓
Execute
```

### Phase 8 — Assembly Intelligence
قابلیت:
- ساخت Assembly
- Insert Components
- Mate
- بررسی تداخل
- تشخیص قطعات
- پیشنهاد اتصال

مثلاً:

> «این دو قطعه را طوری مونتاژ کن که محورشان هم‌راستا باشد.»

### Phase 9 — Drawing & Manufacturing
```
3D Model
   ↓
Drawing
   ↓
Dimensions
   ↓
Manufacturing Information
```

قابلیت‌ها:
- Drawing
- BOM
- اندازه‌گذاری
- خروجی‌های ساخت
- بررسی Manufacturing Constraints

### Phase 10 — Engineering Intelligence
```
Design
 ↓
Engineering Analysis
 ↓
Optimization
```

مثلاً:

> «این طراحی را برای کاهش وزن بررسی کن.»

سیستم می‌تواند پیشنهادهای مهندسی ارائه دهد، اما **نتیجه تحلیل مهندسی را بدون محاسبه/شبیه‌سازی معتبر به‌عنوان حقیقت اعلام نمی‌کند.**

### Phase 11 — Security & Reliability
قبل از Release جدی:

- Permission System
- Command Validation
- Confirmation برای عملیات خطرناک
- Sandbox
- Logging
- Error Recovery
- Offline Safety
- API Key Security
- Privacy Controls

### Phase 12 — Open Source Release (v1.0.0)
```
GitHub
├── Source Code
├── Documentation
├── Installation Guides
├── Examples
├── Tests
├── Releases
└── Contribution Guide
```

با README و آموزش نصب **به ۱۰ زبان**.

---

## ۴. مسیر Milestoneها

```
M0  ██████████  Foundation
M1  ██████████  SolidWorks Add-in
M2  ██████████  AI Core
M3  ██████████  Online AI
M4  ██████████  Offline AI
M5  ██████████  Persian AI
M6  ██████████  CAD Commands
M7  ██████████  Intelligent Modeling
M8  ██████████  Assembly
M9  ██████████  Drawing/Manufacturing
M10 ██████████  Engineering Intelligence
M11 ██████████  Security/Testing
M12 ██████████  Public Release
```

---

## ۵. 🔑 اصل مهم پروژه

از همان روز اول این تفکیک حفظ می‌شود:

```
             AI = تصمیم‌گیری و برنامه‌ریزی
                      │
                      ▼
              CAD Interpreter
                      │
                      ▼
              Command Engine
                      │
                      ▼
             SolidWorks API
                      │
                      ▼
                 SolidWorks
```

این باعث می‌شود بعداً بتوان **مدل AI را عوض کرد، Provider آنلاین را عوض کرد، مدل Ollama را عوض کرد یا حتی قابلیت جدید اضافه کرد — بدون اینکه هسته SolidWorks دوباره نوشته شود.**

---

## ۶. استراتژی نصب و توزیع

هدف نهایی: نصب از طریق **SolidWorks App Store**

یک فایل نصب‌کننده‌ی ساده (`.exe` یا `.msi`) که خودش Add-in را ثبت می‌کند و کاربر چیزی را دستی تنظیم نمی‌کند (به‌جز وارد کردن API key اگر بخواهد از حالت آنلاین استفاده کند).

یعنی کاربر عادی مهندسی، بدون اینکه بداند پشت صحنه چه AI/کد/API‌ای دارد، فقط بازش می‌کند و باهاش حرف می‌زند.

---

## ۷. تجربه‌ی کاربر نهایی

1. **SolidWorks را مثل همیشه باز می‌کند** — هیچ برنامه‌ی جدیدی نصب نمی‌کند، هیچ برنامه‌ی جداگانه‌ای اجرا نمی‌کند.
2. Add-in به‌صورت یک **پنل کناری (Task Pane)** داخل خود SolidWorks ظاهر می‌شود — دقیقاً مثل پنل‌های دیگر SolidWorks (مثل Feature Manager یا Property Manager).
3. یک باکس چت باز می‌کند (راست‌به‌چپ برای فارسی) و می‌نویسد:
   > «یک براکت ۱۰۰ در ۵۰ میلی‌متر بساز و دو سوراخ ۱۰ میلی‌متری بذار»
4. دستیار جواب می‌دهد، عملیات پیشنهادی را نشان می‌دهد (**برای تأیید، نه اجرای خودکار بی‌سروصدا**)، و بعد از تأیید کاربر، خودش در مدل SolidWorks اجرا می‌کند.
