using System;
using System.Collections.Generic;
using Ai.SolidWorksAssistant.Cad;

namespace Ai.SolidWorksAssistant.Tests
{
    /// <summary>Records executed operations; behavior configurable per test.</summary>
    public sealed class FakeExecutor : IPlanExecutor
    {
        public List<CadOperation> Executed { get; } = new List<CadOperation>();

        public Func<CadOperation, OpResult> Behavior = op => OpResult.Success();

        public bool IsMutating(CadOperation op)
        {
            return true;
        }

        public OpResult Execute(CadOperation op)
        {
            Executed.Add(op);
            return Behavior(op);
        }
    }

    /// <summary>Scripted user: approves or rejects; records all reports.</summary>
    public sealed class ScriptedInteractor : IPlanUserInteractor
    {
        public bool Approve = true;
        public List<string> Reports { get; } = new List<string>();
        public List<string> Kinds { get; } = new List<string>();

        public bool ConfirmPlan(CadPlan plan)
        {
            return Approve;
        }

        public void ReportProgress(string message, PlanReportKind kind)
        {
            Reports.Add(message);
            Kinds.Add(kind.ToString());
        }
    }

    public static class TestPlans
    {
        public const string SampleBracket = @"
plan: Test bracket

operation sketch.rectangle
x1 = 0
y1 = 0
x2 = 100
y2 = 50

operation feature.extrude
depth_mm = 10

operation feature.hole
x = 50
y = 25
diameter_mm = 10
through = true
";
    }
}
