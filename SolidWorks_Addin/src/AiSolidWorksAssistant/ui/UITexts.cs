using System;
using System.Threading;

namespace Ai.SolidWorksAssistant.Ui
{
    /// <summary>All user-facing strings (EN/FA) in one place. Localization folder comes in Phase 5.</summary>
    public sealed class UITexts
    {
        public string Language { get; private set; }
        public bool Persian { get; private set; }

        public string TaskPaneTitle { get; private set; }
        public string ChatTitle { get; private set; }
        public string Welcome { get; private set; }
        public string Ready { get; private set; }
        public string Busy { get; private set; }
        public string Send { get; private set; }
        public string Samples { get; private set; }
        public string Approve { get; private set; }
        public string Reject { get; private set; }
        public string ConfirmPrompt { get; private set; }
        public string PlanApproved { get; private set; }
        public string PlanRejected { get; private set; }
        public string You { get; private set; }
        public string Assistant { get; private set; }
        public string System { get; private set; }
        public string UnexpectedError { get; private set; }
        public string ContextHeader { get; private set; }
        public string NoActiveDocument { get; private set; }
        public string NotConnected { get; private set; }
        public string PlanProposedHeader { get; private set; }
        public string ValidationFailed { get; private set; }
        public string OutcomeSucceeded { get; private set; }
        public string OutcomeCancelled { get; private set; }
        public string OutcomeFailed { get; private set; }
        public string EchoNote { get; private set; }
        public string InputHint { get; private set; }

        public string SampleCommandFa { get; private set; }
        public string SampleCommandEn { get; private set; }

        private UITexts(bool persian)
        {
            Persian = persian;
            Language = persian ? "fa" : "en";

            if (persian)
            {
                TaskPaneTitle = "دستیار AI سالیدورک";
                ChatTitle = "گفت‌وگو با دستیار";
                Welcome =
                    "سلام! 👋 من دستیار AI سالیدورک هستم (فاز ۱ — پایه).\n\n" +
                    "می‌تونی به فارسی یا انگلیسی درخواستت رو بنویسی. من:\n" +
                    "• وضعیت فعلی مدل رو می‌خونم و نشونت می‌دم\n" +
                    "• یه «برنامه‌ی عملیات CAD» پیشنهاد می‌دم\n" +
                    "• فقط بعد از تأیید تو، گام‌به‌گام اجراش می‌کنم\n\n" +
                    "نکته: در فاز ۱، هر درخواستی به برنامه‌ی نمونه‌ی پایه تبدیل می‌شه (مستطیل ۱۰۰×۵۰ + اکسترود ۱۰ میلی‌متر + سوراخ ۱۰ میلی‌متری وسط). مغز واقعی AI در فازهای ۲ تا ۴ اضافه می‌شه.";
                Ready = "آماده";
                Busy = "در حال اجرا…";
                Send = "ارسال";
                Samples = "نمونه";
                Approve = "تأیید و اجرا";
                Reject = "لغو";
                ConfirmPrompt = "⚠️ این عملیات روی مدل فعلی اجرا می‌شه. تأیید می‌کنی؟";
                PlanApproved = "✅ تأیید شد — در حال اجرای برنامه…";
                PlanRejected = "⛔ برنامه لغو شد.";
                You = "شما";
                Assistant = "دستیار";
                System = "سیستم";
                UnexpectedError = "خطای غیرمنتظره: ";
                ContextHeader = "📄 وضعیت فعلی مدل:";
                NoActiveDocument = "هیچ سندی باز نیست (یک Part باز کن).";
                NotConnected = "به سالیدورک متصل نیستم.";
                PlanProposedHeader = "برنامه‌ی پیشنهادی:";
                ValidationFailed = "❌ برنامه‌ی پیشنهادی اعتبارسنجی رو رد کرد:";
                OutcomeSucceeded = "✅ اجرا کامل شد: {0} از {1} عملیات انجام شد.";
                OutcomeCancelled = "⛔ اجرا لغو شد.";
                OutcomeFailed = "❌ اجرا متوقف شد در عملیات {0} از {1}: {2}";
                EchoNote = "🔍 حالت نمایشی فاز ۱: برنامه‌ی نمونه پیشنهاد می‌شه.";
                InputHint = "مثلاً: یک براکت ۱۰۰ در ۵۰ بساز و دو سوراخ ۱۰ میلی‌متری بذار…";
                SampleCommandFa = "یک مستطیل ۱۰۰ در ۵۰ میلی‌متر بکش، اکسترودش کن ۱۰ میلی‌متر، و یک سوراخ ۱۰ میلی‌متری وسطش بزن";
                SampleCommandEn = "Draw a 100x50 mm rectangle, extrude it 10 mm, and drill a 10 mm hole in the center.";
            }
            else
            {
                TaskPaneTitle = "AI SolidWorks Assistant";
                ChatTitle = "Assistant chat";
                Welcome =
                    "Hi! 👋 I'm the AI SolidWorks Assistant (Phase 1 — foundation).\n\n" +
                    "Write your request in English or Persian. I will:\n" +
                    "• Read the current model state and show it to you\n" +
                    "• Propose a CAD operation plan\n" +
                    "• Execute it step by step — only after your explicit approval\n\n" +
                    "Note: in Phase 1 every request maps to the built-in sample plan (100×50 rectangle + 10 mm extrude + centered 10 mm hole). The real AI brain arrives in Phases 2–4.";
                Ready = "Ready";
                Busy = "Working…";
                Send = "Send";
                Samples = "Sample";
                Approve = "Approve & run";
                Reject = "Cancel";
                ConfirmPrompt = "⚠️ This will modify the current model. Do you approve?";
                PlanApproved = "✅ Approved — executing plan…";
                PlanRejected = "⛔ Plan cancelled.";
                You = "You";
                Assistant = "Assistant";
                System = "System";
                UnexpectedError = "Unexpected error: ";
                ContextHeader = "📄 Current model state:";
                NoActiveDocument = "No document is open (open a Part first).";
                NotConnected = "Not connected to SOLIDWORKS.";
                PlanProposedHeader = "Proposed plan:";
                ValidationFailed = "❌ The proposed plan failed validation:";
                OutcomeSucceeded = "✅ Done: {0} of {1} operations executed.";
                OutcomeCancelled = "⛔ Execution cancelled.";
                OutcomeFailed = "❌ Execution stopped at operation {0} of {1}: {2}";
                EchoNote = "🔍 Phase 1 demo mode: proposing the sample plan.";
                InputHint = "e.g. create a 100x50 bracket with two 10 mm holes…";
                SampleCommandFa = "یک مستطیل ۱۰۰ در ۵۰ میلی‌متر بکش، اکسترودش کن ۱۰ میلی‌متر، و یک سوراخ ۱۰ میلی‌متری وسطش بزن";
                SampleCommandEn = "Draw a 100x50 mm rectangle, extrude it 10 mm, and drill a 10 mm hole in the center.";
            }
        }

        public static UITexts PersianTexts()
        {
            return new UITexts(true);
        }

        public static UITexts EnglishTexts()
        {
            return new UITexts(false);
        }

        /// <summary>Resolve UI language from a setting value ("fa"/"en"/"auto").</summary>
        public static UITexts Resolve(string setting)
        {
            if (string.Equals(setting, "fa", StringComparison.OrdinalIgnoreCase))
            {
                return PersianTexts();
            }
            if (string.Equals(setting, "en", StringComparison.OrdinalIgnoreCase))
            {
                return EnglishTexts();
            }

            // auto: follow Windows UI language of the SOLIDWORKS machine
            try
            {
                var ui = Thread.CurrentThread.CurrentUICulture;
                for (var depth = 0; ui != null && depth < 10; depth++)
                {
                    if (string.Equals(ui.TwoLetterISOLanguageName, "fa", StringComparison.OrdinalIgnoreCase))
                    {
                        return PersianTexts();
                    }
                    if (string.Equals(ui.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        return EnglishTexts();
                    }
                    if (ui.Parent == ui)
                    {
                        break; // invariant culture marks the end
                    }
                    ui = ui.Parent;
                }
            }
            catch
            {
                // fall through to English
            }
            return EnglishTexts();
        }
    }
}
