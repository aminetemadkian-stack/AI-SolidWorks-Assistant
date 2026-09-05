using System;
using System.Collections.Generic;

namespace Ai.SolidWorksAssistant.Cad
{
    public enum IssueSeverity
    {
        Error,
        Warning
    }

    public sealed class ValidationIssue
    {
        public IssueSeverity Severity { get; private set; }
        public int OperationIndex { get; private set; } // -1 = plan-level
        public string Message { get; private set; }

        public ValidationIssue(IssueSeverity severity, int operationIndex, string message)
        {
            Severity = severity;
            OperationIndex = operationIndex;
            Message = message;
        }
    }

    /// <summary>
    /// The safety gate between AI-provided plans and the SOLIDWORKS runtime.
    /// Nothing reaches the macro generator without passing validation
    /// (the "one rule that shouldn't be broken" from the developer guide).
    /// </summary>
    public static class PlanValidator
    {
        public const double MaxDimensionMm = 10000.0; // sanity bound for Phase 1 demo ops

        public static List<ValidationIssue> Validate(CadPlan plan)
        {
            var issues = new List<ValidationIssue>();
            if (plan == null)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, -1, "Plan is null."));
                return issues;
            }
            if (plan.Operations.Count == 0)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, -1, "Plan contains no operations."));
                return issues;
            }

            var sawSketch = false;

            for (var i = 0; i < plan.Operations.Count; i++)
            {
                var op = plan.Operations[i];

                if (!OpTypes.IsKnown(op.Type))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, i, "Unknown operation type '" + op.Type + "'."));
                    continue;
                }

                if (!OpTypes.IsExecutableInPhase1(op.Type))
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error, i,
                        "Operation '" + op.Type + "' is planned but its execution engine arrives in Phase 6. Remove it from Phase 1 plans."));
                    continue;
                }

                switch (op.Type)
                {
                    case OpTypes.SketchRectangle:
                        CheckPoint(issues, i, op, "x1", "y1");
                        CheckPoint(issues, i, op, "x2", "y2");
                        {
                            var x1 = op.Number("x1"); var y1 = op.Number("y1");
                            var x2 = op.Number("x2"); var y2 = op.Number("y2");
                            if (x1.HasValue && y1.HasValue && x2.HasValue && y2.HasValue &&
                                Math.Abs(x1.Value - x2.Value) < 1e-9 && Math.Abs(y1.Value - y2.Value) < 1e-9)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, i, "Rectangle is degenerate (zero width and height)."));
                            }
                        }
                        sawSketch = true;
                        break;

                    case OpTypes.SketchCircle:
                        CheckPoint(issues, i, op, "x", "y");
                        RequirePositive(issues, i, op, "diameter_mm");
                        sawSketch = true;
                        break;

                    case OpTypes.FeatureExtrude:
                    case OpTypes.FeatureCut:
                        RequirePositive(issues, i, op, "depth_mm");
                        if (!sawSketch)
                        {
                            issues.Add(new ValidationIssue(
                                IssueSeverity.Warning, i,
                                "'" + op.Type + "' runs on the currently active sketch. Make sure a sketch is open (or draw one first in this plan)."));
                        }
                        break;

                    case OpTypes.FeatureHole:
                        CheckPoint(issues, i, op, "x", "y");
                        RequirePositive(issues, i, op, "diameter_mm");
                        break;
                }
            }

            return issues;
        }

        public static bool HasErrors(List<ValidationIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }
            foreach (var issue in issues)
            {
                if (issue.Severity == IssueSeverity.Error)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckPoint(
            List<ValidationIssue> issues, int index, CadOperation op, string xKey, string yKey)
        {
            var x = op.Number(xKey);
            if (!x.HasValue)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, index, "'" + xKey + "' must be a number (got '" + op.Param(xKey) + "')."));
            }
            else
            {
                RequireRange(issues, index, x.Value, xKey);
            }

            var y = op.Number(yKey);
            if (!y.HasValue)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, index, "'" + yKey + "' must be a number (got '" + op.Param(yKey) + "')."));
            }
            else
            {
                RequireRange(issues, index, y.Value, yKey);
            }
        }

        private static void RequirePositive(List<ValidationIssue> issues, int index, CadOperation op, string key)
        {
            var value = op.Number(key);
            if (!value.HasValue)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, index, "'" + key + "' must be a number (got '" + op.Param(key) + "')."));
                return;
            }
            if (value.Value <= 0)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, index, "'" + key + "' must be greater than zero (got " + value.Value + ")."));
                return;
            }
            RequireRange(issues, index, value.Value, key);
        }

        private static void RequireRange(List<ValidationIssue> issues, int index, double value, string key)
        {
            if (Math.Abs(value) > MaxDimensionMm)
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Error, index,
                    "'" + key + "'=" + value + " exceeds the Phase 1 sanity limit of " + MaxDimensionMm + " mm."));
            }
        }
    }
}
