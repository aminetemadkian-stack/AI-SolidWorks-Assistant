using System;
using System.Collections.Generic;
using System.Text;

namespace Ai.SolidWorksAssistant.Cad
{
    // =====================================================================
    // PlanScript — a tiny, human-readable plan format (see cad/PlanScript.md):
    //
    //     plan: Sample bracket
    //
    //     meta
    //     language = en
    //
    //     operation sketch.rectangle
    //     x1 = 0
    //     y1 = 0
    //     x2 = 100
    //     y2 = 50
    //
    // Comments start with '#', '//' or ';'. Markdown noise (---, ```, >) is
    // ignored so plans can live inside markdown documents. Persian/Arabic
    // digits are normalized automatically.
    // =====================================================================

    public sealed class ParseIssue
    {
        public int Line { get; private set; }
        public string Message { get; private set; }

        public ParseIssue(int line, string message)
        {
            Line = line;
            Message = message;
        }

        public override string ToString()
        {
            return "line " + Line + ": " + Message;
        }
    }

    public sealed class ParseResult
    {
        public CadPlan Plan { get; private set; }
        public List<ParseIssue> Errors { get; private set; }
        public List<string> Warnings { get; private set; }

        public bool HasErrors
        {
            get { return Errors.Count > 0; }
        }

        public ParseResult()
        {
            Errors = new List<ParseIssue>();
            Warnings = new List<string>();
        }
    }

    public static class PlanParser
    {
        public static ParseResult Parse(string planText)
        {
            var result = new ParseResult();
            if (string.IsNullOrWhiteSpace(planText))
            {
                result.Errors.Add(new ParseIssue(0, "Plan is empty."));
                return result;
            }

            var plan = new CadPlan();
            var lines = planText.Replace("\r\n", "\n").Split('\n');
            string block = null; // null | "meta" | "operation"
            var operationCount = 0;
            var titleSeen = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = CadOperation.NormalizeDigits(line).Trim();

                if (trimmed.Length == 0 ||
                    trimmed.StartsWith("#", StringComparison.Ordinal) ||
                    trimmed.StartsWith("//", StringComparison.Ordinal) ||
                    trimmed.StartsWith(";", StringComparison.Ordinal) ||
                    trimmed.StartsWith("---", StringComparison.Ordinal) ||
                    trimmed.StartsWith("```", StringComparison.Ordinal) ||
                    trimmed.StartsWith(">", StringComparison.Ordinal))
                {
                    continue;
                }

                // 'plan:' title line
                if (trimmed.StartsWith("plan:", StringComparison.OrdinalIgnoreCase))
                {
                    plan.Title = trimmed.Substring(5).Trim();
                    titleSeen = true;
                    block = null;
                    continue;
                }

                // block headers
                if (trimmed.Equals("meta", StringComparison.OrdinalIgnoreCase))
                {
                    block = "meta";
                    continue;
                }

                if (trimmed.StartsWith("operation", StringComparison.OrdinalIgnoreCase))
                {
                    var type = trimmed.Substring(9).Trim().TrimStart(':').Trim();
                    if (type.Length == 0)
                    {
                        result.Errors.Add(new ParseIssue(i + 1, "'operation' header is missing a type (e.g. 'operation sketch.rectangle')."));
                        block = null;
                        continue;
                    }
                    type = type.ToLowerInvariant();

                    plan.Operations.Add(new CadOperation(type, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
                    operationCount++;
                    block = "operation";
                    continue;
                }

                // key = value (or key: value)
                var eq = trimmed.IndexOf('=');
                var colon = trimmed.IndexOf(':');
                int sep;
                if (eq >= 0 && (colon < 0 || eq < colon))
                {
                    sep = eq;
                }
                else if (colon >= 0)
                {
                    sep = colon;
                }
                else
                {
                    result.Warnings.Add("line " + (i + 1) + ": ignored unrecognized text — " + trimmed);
                    continue;
                }

                var key = trimmed.Substring(0, sep).Trim();
                var value = trimmed.Substring(sep + 1).Trim();

                if (key.Length == 0)
                {
                    result.Warnings.Add("line " + (i + 1) + ": ignored empty key.");
                    continue;
                }

                if (block == "meta")
                {
                    plan.Meta[key] = value;
                    continue;
                }

                if (block == "operation" && plan.Operations.Count > 0)
                {
                    plan.Operations[plan.Operations.Count - 1].Params[key] = value;
                    continue;
                }

                // key/value outside any block and before any title — treat as meta
                plan.Meta[key] = value;
            }

            if (!titleSeen)
            {
                result.Warnings.Add("Plan has no 'plan: <title>' line.");
            }

            if (operationCount == 0)
            {
                result.Errors.Add(new ParseIssue(0, "Plan contains no operations. Add at least one 'operation <type>' block."));
            }

            result.Plan = plan;
            return result;
        }

        /// <summary>Short one-line description of an operation for UI summaries.</summary>
        public static string Describe(CadOperation op, bool persian)
        {
            if (op == null)
            {
                return "?";
            }

            var n = new Func<string, double?>(key => op.Number(key));

            switch (op.Type)
            {
                case OpTypes.SketchRectangle:
                {
                    var x1 = n("x1"); var y1 = n("y1"); var x2 = n("x2"); var y2 = n("y2");
                    return persian
                        ? string.Format("رسم مستطیل ({0},{1}) → ({2},{3}) میلی‌متر", F(x1), F(y1), F(x2), F(y2))
                        : string.Format("Rectangle ({0},{1}) → ({2},{3}) mm", F(x1), F(y1), F(x2), F(y2));
                }
                case OpTypes.SketchCircle:
                {
                    var x = n("x"); var y = n("y"); var d = n("diameter_mm");
                    return persian
                        ? string.Format("رسم دایره در ({0},{1}) با قطر {2} میلی‌متر", F(x), F(y), F(d))
                        : string.Format("Circle at ({0},{1}), diameter {2} mm", F(x), F(y), F(d));
                }
                case OpTypes.FeatureExtrude:
                {
                    var depth = n("depth_mm");
                    return persian
                        ? string.Format("اکسترود به عمق {0} میلی‌متر", F(depth))
                        : string.Format("Extrude, depth {0} mm", F(depth));
                }
                case OpTypes.FeatureCut:
                {
                    var depth = n("depth_mm");
                    return persian
                        ? string.Format("برش به عمق {0} میلی‌متر", F(depth))
                        : string.Format("Cut, depth {0} mm", F(depth));
                }
                case OpTypes.FeatureHole:
                {
                    var x = n("x"); var y = n("y"); var d = n("diameter_mm");
                    var through = op.Flag("through");
                    var throughTxt = (through ?? false)
                        ? (persian ? "عبور از تمام" : "through all")
                        : (persian ? "کور" : "blind");
                    return persian
                        ? string.Format("سوراخ Ø{0} میلی‌متر در ({1},{2}) — {3}", F(d), F(x), F(y), throughTxt)
                        : string.Format("Hole Ø{0} mm at ({1},{2}) — {3}", F(d), F(x), F(y), throughTxt);
                }
                case OpTypes.FeatureFillet:
                {
                    var r = n("radius_mm");
                    return persian
                        ? string.Format("فیلت با شعاع {0} میلی‌متر (نیازمند موتور فرمان فاز ۶)", F(r))
                        : string.Format("Fillet r={0} mm (requires Phase 6 command engine)", F(r));
                }
                case OpTypes.FeatureChamfer:
                {
                    var d = n("distance_mm");
                    return persian
                        ? string.Format("پخ به اندازه {0} میلی‌متر (نیازمند موتور فرمان فاز ۶)", F(d))
                        : string.Format("Chamfer {0} mm (requires Phase 6 command engine)", F(d));
                }
                default:
                    return op.Type;
            }
        }

        private static string F(double? value)
        {
            if (!value.HasValue)
            {
                return "?";
            }
            return value.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
