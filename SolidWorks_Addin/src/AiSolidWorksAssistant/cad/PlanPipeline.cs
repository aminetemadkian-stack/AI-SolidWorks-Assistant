using System;
using System.Collections.Generic;

namespace Ai.SolidWorksAssistant.Cad
{
    public enum PlanReportKind
    {
        Info,
        Progress,
        Error
    }

    /// <summary>
    /// How the outside world (chat UI / tests) interacts with a running plan:
    /// approval gate + progress reporting. Implemented by ChatViewModel in the UI
    /// and by scripted fakes in tests.
    /// </summary>
    public interface IPlanUserInteractor
    {
        /// <summary>Ask the user to approve the whole plan. Return false to cancel.</summary>
        bool ConfirmPlan(CadPlan plan);

        /// <summary>Progress / error reporting during plan execution.</summary>
        void ReportProgress(string message, PlanReportKind kind);
    }

    public enum PlanStatus
    {
        Succeeded,
        Cancelled,
        Failed
    }

    public sealed class PlanOutcome
    {
        public PlanStatus Status { get; private set; }
        public int CompletedOperations { get; private set; }
        public int TotalOperations { get; private set; }
        public string Error { get; private set; }

        public PlanOutcome(PlanStatus status, int completed, int total, string error)
        {
            Status = status;
            CompletedOperations = completed;
            TotalOperations = total;
            Error = error;
        }
    }

    /// <summary>
    /// Executes a validated plan step by step.
    ///
    /// Safety contract (Phase 1):
    ///   - plans must pass PlanValidator first (caller responsibility);
    ///   - nothing executes without explicit user approval;
    ///   - the first failing operation stops the plan (no half-finished silent states);
    ///   - undo granularity = one SOLIDWORKS undo step per executed macro
    ///     (plan-level undo grouping arrives with the Phase 6 native command engine).
    /// </summary>
    public static class PlanPipeline
    {
        public static PlanOutcome Execute(CadPlan plan, IPlanExecutor executor, IPlanUserInteractor interactor)
        {
            if (plan == null)
            {
                throw new ArgumentNullException("plan");
            }
            if (executor == null)
            {
                throw new ArgumentNullException("executor");
            }
            if (interactor == null)
            {
                throw new ArgumentNullException("interactor");
            }

            var total = plan.Operations.Count;

            if (!interactor.ConfirmPlan(plan))
            {
                interactor.ReportProgress("Plan cancelled by the user.", PlanReportKind.Info);
                return new PlanOutcome(PlanStatus.Cancelled, 0, total, null);
            }

            var done = 0;
            for (var i = 0; i < total; i++)
            {
                var op = plan.Operations[i];
                interactor.ReportProgress(
                    "Executing operation " + (i + 1) + "/" + total + ": " + op.Type,
                    PlanReportKind.Progress);

                try
                {
                    var result = executor.Execute(op);
                    if (result == null || !result.Ok)
                    {
                        var error = result == null ? "Executor returned no result." : result.Error;
                        interactor.ReportProgress(
                            "Operation " + (i + 1) + "/" + total + " failed: " + error,
                            PlanReportKind.Error);
                        return new PlanOutcome(PlanStatus.Failed, done, total, error);
                    }
                }
                catch (Exception ex)
                {
                    interactor.ReportProgress(
                        "Operation " + (i + 1) + "/" + total + " crashed: " + ex.Message,
                        PlanReportKind.Error);
                    return new PlanOutcome(PlanStatus.Failed, done, total, ex.Message);
                }

                done++;
            }

            interactor.ReportProgress(
                "All " + total + " operations executed.", PlanReportKind.Info);
            return new PlanOutcome(PlanStatus.Succeeded, done, total, null);
        }
    }
}
