using System;
using System.Globalization;
using System.Text;
using Ai.SolidWorksAssistant.Addin;

namespace Ai.SolidWorksAssistant.Cad
{
    /// <summary>A generated SOLIDWORKS VBA macro for a single operation.</summary>
    public sealed class MacroScript
    {
        public string FileNameBase { get; private set; }
        public string Code { get; private set; }
        public string Description { get; private set; }

        public MacroScript(string fileNameBase, string code, string description)
        {
            FileNameBase = fileNameBase;
            Code = code;
            Description = description;
        }
    }

    /// <summary>
    /// Compiles one validated CadOperation into a standalone SOLIDWORKS VBA macro (.swb).
    /// Phase 1 executes through VBA macros; the Phase 6 command engine will call the
    /// SOLIDWORKS API natively from C# with the same operation schema.
    ///
    /// Units: SOLIDWORKS API lengths are METERS. PlanScript values are MILLIMETERS,
    /// converted here (÷1000) with invariant literals.
    /// </summary>
    public static class SwMacroGenerator
    {
        public static MacroScript Generate(CadOperation op)
        {
            if (op == null)
            {
                throw new ArgumentNullException("op");
            }

            if (!OpTypes.IsExecutableInPhase1(op.Type))
            {
                throw new NotSupportedException(
                    "Operation '" + op.Type + "' has no Phase 1 runtime. It will become executable with the Phase 6 CAD command engine.");
            }

            string body;
            var description = PlanParser.Describe(op, false);

            switch (op.Type)
            {
                case OpTypes.SketchRectangle:
                    body = EmitPlaneSelect(op, "Front Plane")
                         + EmitInsertSketch()
                         + "    swModel.SketchManager.CreateCornerRectangle "
                            + M(op, "x1") + ", " + M(op, "y1") + ", 0#, "
                            + M(op, "x2") + ", " + M(op, "y2") + ", 0#\n"
                         + EmitInsertSketch();
                    break;

                case OpTypes.SketchCircle:
                    body = EmitPlaneSelect(op, "Front Plane")
                         + EmitInsertSketch()
                         + "    swModel.SketchManager.CreateCircleByRadius "
                            + M(op, "x") + ", " + M(op, "y") + ", 0#, "
                            + RadiusLiteral(op) + "\n"
                         + EmitInsertSketch();
                    break;

                case OpTypes.FeatureExtrude:
                    body = "    Dim swFeat As Object\n"
                         + "    Set swFeat = swModel.FeatureManager.FeatureExtrusion3(" +
                           "True, False, False, " + EndCond(op, SwConst.swEndCondBlind) + ", 0, "
                           + DepthLiteral(op, "depth_mm") + ", 0#, " +
                           "False, False, False, False, 0#, 0#, False, False, False, False, " +
                           "True, True, True, 0, 0#, False)\n"
                         + "    If swFeat Is Nothing Then Err.Raise vbObjectError + 1004, , \"Extrude failed. Open a sketch with a closed profile before extruding.\"\n";
                    break;

                case OpTypes.FeatureCut:
                    body = "    Dim swFeat As Object\n"
                         + "    Set swFeat = swModel.FeatureManager.FeatureCut3(" +
                           "True, False, False, " + EndCond(op, SwConst.swEndCondBlind) + ", 0, "
                           + DepthLiteral(op, "depth_mm") + ", 0#, " +
                           "False, False, False, False, 0#, 0#, False, False, False, False, " +
                           "True, True, True, 0, 0#, False)\n"
                         + "    If swFeat Is Nothing Then Err.Raise vbObjectError + 1005, , \"Cut failed. Open a sketch with a closed profile before cutting.\"\n";
                    break;

                case OpTypes.FeatureHole:
                    body = EmitPlaneSelect(op, op.Param("plane") ?? "Front Plane")
                         + EmitInsertSketch()
                         + "    swModel.SketchManager.CreateCircleByRadius "
                            + M(op, "x") + ", " + M(op, "y") + ", 0#, "
                            + RadiusFromDiameter(op, "diameter_mm") + "\n"
                         + "    Dim swFeat As Object\n"
                         + "    Set swFeat = swModel.FeatureManager.FeatureCut3(" +
                           "True, False, False, " + EndCond(op, SwConst.swEndCondThroughAll) + ", 0, "
                           + "0#, 0#, " +
                           "False, False, False, False, 0#, 0#, False, False, False, False, " +
                           "True, True, True, 0, 0#, False)\n"
                         + "    If swFeat Is Nothing Then Err.Raise vbObjectError + 1006, , \"Hole (cut) failed.\"\n"
                         + EmitInsertSketch();
                    break;

                default:
                    throw new NotSupportedException("Unexpected operation type '" + op.Type + "'.");
            }

            var fileNameBase = "AiSWA_" + Sanitize(op.Type) + "_" +
                DateTime.UtcNow.ToString("HHmmss", CultureInfo.InvariantCulture);

            var code = BuildMacro(body, description);
            return new MacroScript(fileNameBase, code, description);
        }

        private static string BuildMacro(string body, string description)
        {
            var sb = new StringBuilder();
            sb.Append("' Generated by AI SolidWorks Assistant (Phase 1) — do not edit.\n");
            sb.Append("' Operation: ").Append(description.Replace('\n', ' ')).Append('\n');
            sb.Append("Dim swApp As Object\n");
            sb.Append("Dim swModel As Object\n");
            sb.Append("\nSub main()\n");
            sb.Append("    On Error GoTo Fail\n");
            sb.Append("    Set swApp = Application.SldWorks\n");
            sb.Append("    If swApp Is Nothing Then Err.Raise vbObjectError + 1001, , \"SOLIDWORKS application not available.\"\n");
            sb.Append("    Set swModel = swApp.ActiveDoc\n");
            sb.Append("    If swModel Is Nothing Then Err.Raise vbObjectError + 1002, , \"No active document. Open or create a part first.\"\n");
            sb.Append(body);
            sb.Append("    Exit Sub\n");
            sb.Append("Fail:\n");
            sb.Append("    MsgBox \"AI Assistant: \" & Err.Description, vbExclamation, \"AI SolidWorks Assistant\"\n");
            sb.Append("End Sub\n");
            return sb.ToString();
        }

        private static string EmitPlaneSelect(CadOperation op, string defaultPlane)
        {
            var plane = op.Param("plane");
            if (string.IsNullOrWhiteSpace(plane))
            {
                plane = defaultPlane;
            }
            var safe = plane.Replace("\"", "'");
            return "    If swModel.Extension.SelectByID2(\"" + safe + "\", \"PLANE\", 0#, 0#, 0#, False, 0, Nothing, 0) = False Then\n"
                 + "        Err.Raise vbObjectError + 1003, , \"Could not select plane '" + safe + "'. Note: plane names are localized in non-English SOLIDWORKS installations.\"\n"
                 + "    End If\n";
        }

        private static string EmitInsertSketch()
        {
            return "    swModel.SketchManager.InsertSketch True\n";
        }

        /// <summary>Millimeters → meters literal (invariant).</summary>
        private static string MillimetersToMeters(double mm)
        {
            return (mm / 1000.0).ToString("R", CultureInfo.InvariantCulture);
        }

        private static string M(CadOperation op, string key)
        {
            var value = op.Number(key);
            if (!value.HasValue)
            {
                throw new InvalidOperationException(
                    "Operation '" + op.Type + "' requires numeric '" + key + "'. Did the plan pass validation?");
            }
            return MillimetersToMeters(value.Value);
        }

        private static string DepthLiteral(CadOperation op, string key)
        {
            // Through-all ops ignore depth; emit 0 so blind depth is still validated when used.
            var through = op.Flag("through");
            if (through.HasValue && through.Value)
            {
                return "0#";
            }
            return M(op, key);
        }

        private static string RadiusLiteral(CadOperation op)
        {
            var diameter = op.Number("diameter_mm");
            if (diameter.HasValue)
            {
                return MillimetersToMeters(diameter.Value / 2.0);
            }
            return M(op, "radius_mm");
        }

        private static string RadiusFromDiameter(CadOperation op, string diameterKey)
        {
            var diameter = op.Number(diameterKey);
            if (!diameter.HasValue)
            {
                throw new InvalidOperationException(
                    "Operation '" + op.Type + "' requires numeric '" + diameterKey + "'.");
            }
            return MillimetersToMeters(diameter.Value / 2.0);
        }

        private static string EndCond(CadOperation op, int blindValue)
        {
            var through = op.Flag("through");
            if (through.HasValue && through.Value)
            {
                return SwConst.swEndCondThroughAll.ToString(CultureInfo.InvariantCulture);
            }
            return blindValue.ToString(CultureInfo.InvariantCulture);
        }

        private static string Sanitize(string type)
        {
            var sb = new StringBuilder();
            foreach (var ch in type)
            {
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            return sb.ToString();
        }
    }
}
