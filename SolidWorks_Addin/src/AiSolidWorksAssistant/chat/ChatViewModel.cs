using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ai.SolidWorksAssistant.Cad;
using Ai.SolidWorksAssistant.Core;
using Ai.SolidWorksAssistant.Core.ModelContext;
using Ai.SolidWorksAssistant.Ui;

namespace Ai.SolidWorksAssistant.Chat
{
    /// <summary>
    /// Chat orchestration: user input → model context → intent engine → plan parse →
    /// validate → confirm → execute → report. Framework-free so it is fully unit-testable.
    ///
    /// IMPORTANT: when interactive confirmation is enabled (default), HandleUserInput
    /// must be called on a background thread — ConfirmPlan blocks the caller until the
    /// user clicks Approve/Reject in the UI. The WinForms view takes care of this.
    /// </summary>
    public sealed class ChatViewModel : IPlanUserInteractor
    {
        private readonly IIntentEngine _engine;
        private readonly IModelContextReader _contextReader;
        private readonly IPlanExecutor _executor;
        private readonly UITexts _t;

        private readonly object _gate = new object();
        private TaskCompletionSource<bool> _confirmationTcs;
        private bool _busy;

        public event Action<ChatMessage> MessageAdded;
        public event Action<bool> BusyChanged;
        public event Action<bool> ConfirmationPendingChanged;
        public event Action<string> StatusChanged;

        public List<ChatMessage> Messages { get; private set; }

        /// <summary>Test/automation switch: auto-approve plans without UI round-trip.</summary>
        public bool AutoApprove { get; set; }

        public UITexts Texts
        {
            get { return _t; }
        }

        public ChatViewModel(
            IIntentEngine engine,
            IModelContextReader contextReader,
            IPlanExecutor executor,
            UITexts texts)
        {
            if (engine == null)
            {
                throw new ArgumentNullException("engine");
            }
            if (executor == null)
            {
                throw new ArgumentNullException("executor");
            }
            if (texts == null)
            {
                throw new ArgumentNullException("texts");
            }
            _engine = engine;
            _contextReader = contextReader; // optional in tests
            _executor = executor;
            _t = texts;

            Messages = new List<ChatMessage>();
        }

        public void PostWelcome()
        {
            AddMessage(ChatRole.System, _t.Welcome);
        }

        public void HandleUserInput(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _busy)
            {
                return;
            }

            SetBusy(true);
            try
            {
                AddMessage(ChatRole.User, text.Trim());

                // 1) Model context (read-only snapshot)
                ModelSnapshot context = null;
                if (_contextReader != null)
                {
                    try
                    {
                        context = _contextReader.Read();
                        AddMessage(ChatRole.System, ContextFormatter.Format(context, _t));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Context read failed: " + ex.Message);
                    }
                }

                // 2) Intent engine (Phase 1: echo → sample plan)
                EngineResponse response;
                try
                {
                    response = _engine.Process(text.Trim(), context);
                }
                catch (Exception ex)
                {
                    Log.Error("Intent engine failed", ex);
                    AddMessage(ChatRole.Assistant, _t.UnexpectedError + ex.Message);
                    return;
                }

                // 3) Parse the proposed plan
                var parsed = PlanParser.Parse(response.ProposedPlanMarkdown);
                if (parsed.HasErrors)
                {
                    var sb = new StringBuilder(_t.ValidationFailed);
                    foreach (var error in parsed.Errors)
                    {
                        sb.Append("\n  • ").Append(error);
                    }
                    AddMessage(ChatRole.Assistant, sb.ToString());
                    return;
                }
                foreach (var warning in parsed.Warnings)
                {
                    AddMessage(ChatRole.System, "⚠ " + warning);
                }

                var plan = parsed.Plan;

                // 4) Validate the plan (the safety gate)
                var issues = PlanValidator.Validate(plan);
                if (PlanValidator.HasErrors(issues))
                {
                    var sb = new StringBuilder(_t.ValidationFailed);
                    foreach (var issue in issues)
                    {
                        if (issue.Severity == IssueSeverity.Error)
                        {
                            sb.Append("\n  • ").Append(issue.Message);
                        }
                    }
                    AddMessage(ChatRole.Assistant, sb.ToString());
                    return;
                }
                foreach (var issue in issues)
                {
                    if (issue.Severity == IssueSeverity.Warning)
                    {
                        AddMessage(ChatRole.System, "⚠ " + issue.Message);
                    }
                }

                // 5) Present the plan
                var reply = new StringBuilder();
                reply.Append(_t.PlanProposedHeader).Append('\n');
                var number = 1;
                foreach (var op in plan.Operations)
                {
                    reply.Append(number++).Append(". ").Append(PlanParser.Describe(op, _t.Persian)).Append('\n');
                }
                reply.Append('\n').Append(_t.EchoNote);
                AddMessage(ChatRole.Assistant, reply.ToString());

                // 6) Execute through the pipeline (confirm → run → report)
                var outcome = PlanPipeline.Execute(plan, _executor, this);
                AddMessage(ChatRole.System, OutcomeFormatter.Format(outcome, _t));
            }
            catch (Exception ex)
            {
                Log.Error("HandleUserInput failed", ex);
                try
                {
                    AddMessage(ChatRole.Assistant, _t.UnexpectedError + ex.Message);
                }
                catch
                {
                    // last-resort guard
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>User clicked the Approve button.</summary>
        public void ApprovePlan()
        {
            var tcs = TakeConfirmationTcs();
            if (tcs != null)
            {
                AddMessage(ChatRole.System, _t.PlanApproved);
                tcs.TrySetResult(true);
            }
        }

        /// <summary>User clicked the Reject button.</summary>
        public void RejectPlan()
        {
            var tcs = TakeConfirmationTcs();
            if (tcs != null)
            {
                AddMessage(ChatRole.System, _t.PlanRejected);
                tcs.TrySetResult(false);
            }
        }

        public void Shutdown()
        {
            var tcs = TakeConfirmationTcs();
            if (tcs != null)
            {
                tcs.TrySetResult(false);
            }
        }

        // ---- IPlanUserInteractor ----

        bool IPlanUserInteractor.ConfirmPlan(CadPlan plan)
        {
            if (AutoApprove)
            {
                return true;
            }

            var sb = new StringBuilder();
            sb.Append(_t.ConfirmPrompt).Append("\n\n");
            var number = 1;
            foreach (var op in plan.Operations)
            {
                sb.Append(number++).Append(". ").Append(PlanParser.Describe(op, _t.Persian)).Append('\n');
            }
            AddMessage(ChatRole.Assistant, sb.ToString(), isConfirmation: true);

            var tcs = new TaskCompletionSource<bool>();
            lock (_gate)
            {
                _confirmationTcs = tcs;
            }
            RaiseConfirmationPending(true);

            // Blocks the background worker until the user clicks Approve/Reject.
            return tcs.Task.GetAwaiter().GetResult();
        }

        void IPlanUserInteractor.ReportProgress(string message, PlanReportKind kind)
        {
            RaiseStatus(message);
            if (kind == PlanReportKind.Error)
            {
                AddMessage(ChatRole.System, "❌ " + message);
            }
        }

        // ---- helpers ----

        private TaskCompletionSource<bool> TakeConfirmationTcs()
        {
            TaskCompletionSource<bool> tcs;
            lock (_gate)
            {
                tcs = _confirmationTcs;
                _confirmationTcs = null;
            }
            RaiseConfirmationPending(false);
            return tcs;
        }

        private void RaiseConfirmationPending(bool pending)
        {
            var handler = ConfirmationPendingChanged;
            if (handler != null)
            {
                handler(pending);
            }
        }

        private void RaiseStatus(string message)
        {
            var handler = StatusChanged;
            if (handler != null)
            {
                handler(message);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            RaiseStatus(busy ? _t.Busy : _t.Ready);
            var handler = BusyChanged;
            if (handler != null)
            {
                handler(busy);
            }
        }

        private void AddMessage(ChatRole role, string text, bool isConfirmation = false)
        {
            var message = new ChatMessage(role, text, isConfirmation);
            lock (_gate)
            {
                Messages.Add(message);
            }
            var handler = MessageAdded;
            if (handler != null)
            {
                handler(message);
            }
        }
    }

    // ------------------------------------------------------------------
    // Formatting helpers (also used by tests)
    // ------------------------------------------------------------------

    public static class ContextFormatter
    {
        public static string Format(ModelSnapshot snapshot, UITexts t)
        {
            var sb = new StringBuilder();
            sb.Append(t.ContextHeader).Append('\n');

            if (snapshot == null)
            {
                sb.Append(t.NotConnected);
                return sb.ToString();
            }

            if (!snapshot.IsConnected)
            {
                sb.Append(t.NotConnected);
                return sb.ToString();
            }

            if (!snapshot.HasActiveDocument)
            {
                sb.Append(t.NoActiveDocument);
                return sb.ToString();
            }

            var title = string.IsNullOrEmpty(snapshot.DocumentTitle) ? "?" : snapshot.DocumentTitle;
            var type = string.IsNullOrEmpty(snapshot.DocumentType) ? "?" : snapshot.DocumentType;
            sb.Append(title).Append(" (").Append(type).Append(")\n");

            if (!string.IsNullOrEmpty(snapshot.ConfigurationName))
            {
                sb.Append("⚙ ").Append(snapshot.ConfigurationName);
                if (snapshot.ConfigurationCount.HasValue && snapshot.ConfigurationCount.Value > 1)
                {
                    sb.Append("  (").Append(snapshot.ConfigurationCount.Value).Append(')');
                }
                sb.Append('\n');
            }

            if (snapshot.SketchActive.HasValue && snapshot.SketchActive.Value)
            {
                sb.Append(t.Persian ? "✏ اسکچ باز است\n" : "✏ A sketch is active\n");
            }

            if (snapshot.SelectedObjectCount.HasValue && snapshot.SelectedObjectCount.Value > 0)
            {
                sb.Append(t.Persian
                    ? "🖱 انتخاب‌شده: " + snapshot.SelectedObjectCount.Value + '\n'
                    : "🖱 Selected: " + snapshot.SelectedObjectCount.Value + '\n');
            }

            if (snapshot.Features != null && snapshot.Features.Count > 0)
            {
                sb.Append(t.Persian
                    ? "🌳 فیچرها (" + snapshot.Features.Count + "): "
                    : "🌳 Features (" + snapshot.Features.Count + "): ");
                var shown = Math.Min(snapshot.Features.Count, 8);
                for (var i = 0; i < shown; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(snapshot.Features[i].Name);
                }
                if (snapshot.Features.Count > shown)
                {
                    sb.Append(", …");
                }
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }

    public static class OutcomeFormatter
    {
        public static string Format(PlanOutcome outcome, UITexts t)
        {
            if (outcome == null)
            {
                return "?";
            }

            switch (outcome.Status)
            {
                case PlanStatus.Succeeded:
                    return string.Format(t.OutcomeSucceeded, outcome.CompletedOperations, outcome.TotalOperations);
                case PlanStatus.Cancelled:
                    return t.OutcomeCancelled;
                default:
                    return string.Format(t.OutcomeFailed, outcome.CompletedOperations + 1, outcome.TotalOperations, outcome.Error);
            }
        }
    }
}
