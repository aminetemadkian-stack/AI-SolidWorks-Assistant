using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ai.SolidWorksAssistant.Cad
{
    /// <summary>Operation type vocabulary (subset implemented; full set arrives in Phase 6).</summary>
    public static class OpTypes
    {
        public const string SketchRectangle = "sketch.rectangle";
        public const string SketchCircle = "sketch.circle";
        public const string FeatureExtrude = "feature.extrude";
        public const string FeatureCut = "feature.cut";
        public const string FeatureHole = "feature.hole";

        // Parsed & validated, but execution arrives with the Phase 6 CAD command engine.
        public const string FeatureFillet = "feature.fillet";
        public const string FeatureChamfer = "feature.chamfer";
        public const string FeaturePattern = "feature.pattern";
        public const string FeatureMirror = "feature.mirror";
        public const string AssemblyMate = "assembly.mate";
        public const string AssemblyInsert = "assembly.insert";

        public static bool IsKnown(string type)
        {
            switch (type)
            {
                case SketchRectangle:
                case SketchCircle:
                case FeatureExtrude:
                case FeatureCut:
                case FeatureHole:
                case FeatureFillet:
                case FeatureChamfer:
                case FeaturePattern:
                case FeatureMirror:
                case AssemblyMate:
                case AssemblyInsert:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Operations the Phase 1 runtime can actually execute in SOLIDWORKS.</summary>
        public static bool IsExecutableInPhase1(string type)
        {
            switch (type)
            {
                case SketchRectangle:
                case SketchCircle:
                case FeatureExtrude:
                case FeatureCut:
                case FeatureHole:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// One structured CAD operation. The AI layer may only ever produce these —
    /// they are validated before anything touches SOLIDWORKS (architecture golden rule).
    /// </summary>
    public sealed class CadOperation
    {
        public string Type { get; private set; }
        public IDictionary<string, string> Params { get; private set; }

        public CadOperation(string type, IDictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException("Operation type is required.", "type");
            }
            Type = type.Trim().ToLowerInvariant();
            Params = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Param(string key)
        {
            string value;
            return Params.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>Parse a numeric parameter. Accepts "10", "۱۰", "10 mm", "10.5", "۱۰٫۵".</summary>
        public double? Number(string key)
        {
            return NumberParam(Param(key));
        }

        public bool? Flag(string key)
        {
            var raw = Param(key);
            if (raw == null)
            {
                return null;
            }
            raw = NormalizeDigits(raw).Trim().ToLowerInvariant();
            if (raw == "true" || raw == "yes" || raw == "1" || raw == "بله")
            {
                return true;
            }
            if (raw == "false" || raw == "no" || raw == "0" || raw == "نه" || raw == "خیر")
            {
                return false;
            }
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Type);
            foreach (var kvp in Params)
            {
                sb.Append(' ').Append(kvp.Key).Append('=').Append(kvp.Value);
            }
            return sb.ToString();
        }

        // ---- shared value parsing helpers (used by the parser too) ----

        public static double? NumberParam(string raw)
        {
            if (raw == null)
            {
                return null;
            }

            var cleaned = NormalizeDigits(raw).Trim();

            // Strip a trailing unit word: "10 mm" / "10mm"
            var cut = cleaned.IndexOf(' ');
            if (cut > 0)
            {
                cleaned = cleaned.Substring(0, cut).Trim();
            }

            // Treat ',' and '٫' as decimal/thousand separators — drop thousands, keep decimal point.
            cleaned = cleaned.Replace("٫", ".");
            var lastDot = cleaned.LastIndexOf('.');
            var lastComma = cleaned.LastIndexOf(',');
            if (lastComma > lastDot)
            {
                cleaned = cleaned.Substring(0, lastComma) + "." + cleaned.Substring(lastComma + 1);
            }
            cleaned = cleaned.Replace(",", "");

            double result;
            if (double.TryParse(
                    cleaned,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }
            return null;
        }

        /// <summary>Converts Persian (۰-۹) and Arabic-Indic (٠-٩) digits to ASCII.</summary>
        public static string NormalizeDigits(string text)
        {
            if (text == null)
            {
                return null;
            }

            var changed = false;
            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                var c = (int)ch;
                if (c >= 0x06F0 && c <= 0x06F9) // Persian
                {
                    sb.Append((char)('0' + (c - 0x06F0)));
                    changed = true;
                }
                else if (c >= 0x0660 && c <= 0x0669) // Arabic-Indic
                {
                    sb.Append((char)('0' + (c - 0x0660)));
                    changed = true;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return changed ? sb.ToString() : text;
        }
    }

    /// <summary>A parsed plan: ordered list of validated-then-executed CAD operations.</summary>
    public sealed class CadPlan
    {
        public string Title { get; set; }
        public IDictionary<string, string> Meta { get; private set; }
        public List<CadOperation> Operations { get; private set; }

        public CadPlan()
        {
            Title = "(untitled plan)";
            Meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Operations = new List<CadOperation>();
        }
    }
}
