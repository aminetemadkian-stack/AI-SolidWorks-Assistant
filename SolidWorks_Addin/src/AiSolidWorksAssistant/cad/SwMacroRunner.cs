using System;
using System.IO;
using Ai.SolidWorksAssistant.Core;

namespace Ai.SolidWorksAssistant.Cad
{
    public sealed class MacroRunResult
    {
        public bool Ok { get; private set; }
        public string Error { get; private set; }

        public MacroRunResult(bool ok, string error)
        {
            Ok = ok;
            Error = error;
        }
    }

    public interface ISwMacroRunner
    {
        MacroRunResult Run(MacroScript macro);
    }

    public sealed class OpResult
    {
        public bool Ok { get; private set; }
        public string Error { get; private set; }

        public OpResult(bool ok, string error)
        {
            Ok = ok;
            Error = error;
        }

        public static OpResult Success()
        {
            return new OpResult(true, null);
        }

        public static OpResult Fail(string error)
        {
            return new OpResult(false, error);
        }
    }

    /// <summary>Executes a single validated CAD operation against SOLIDWORKS.</summary>
    public interface IPlanExecutor
    {
        bool IsMutating(CadOperation op);
        OpResult Execute(CadOperation op);
    }

    /// <summary>
    /// Writes the generated macro to a temp .swb file and asks SOLIDWORKS to run it
    /// (RunMacro2 via IDispatch late binding, with a RunMacro fallback).
    /// </summary>
    public sealed class SwMacroRunner : ISwMacroRunner
    {
        private readonly Func<object> _swAppProvider;

        public SwMacroRunner(Func<object> swAppProvider)
        {
            if (swAppProvider == null)
            {
                throw new ArgumentNullException("swAppProvider");
            }
            _swAppProvider = swAppProvider;
        }

        public MacroRunResult Run(MacroScript macro)
        {
            if (macro == null)
            {
                return new MacroRunResult(false, "Macro script is null.");
            }

            var app = _swAppProvider();
            if (app == null)
            {
                return new MacroRunResult(false, "Not connected to SOLIDWORKS.");
            }

            var path = Path.Combine(
                Path.GetTempPath(),
                macro.FileNameBase + ".swb");

            try
            {
                File.WriteAllText(path, macro.Code, System.Text.Encoding.Unicode);

                // RunMacro2(FileName, ModuleName, ProcedureName, Options, ByRef Error)
                var module = Path.GetFileNameWithoutExtension(path);
                var args = new object[] { path, module, "main", 0, 0 };
                ComLate.CallWithByRef(app, "RunMacro2", args, new[] { 4 });

                var error = Convert.ToInt32(args[4]);
                if (error != 0)
                {
                    return new MacroRunResult(
                        false,
                        "SOLIDWORKS could not run the macro (RunMacro2 error code " + error + ").");
                }

                return new MacroRunResult(true, null);
            }
            catch (Exception ex2)
            {
                Log.Warn("RunMacro2 failed (" + ex2.Message + "); trying RunMacro fallback.");

                try
                {
                    var module = Path.GetFileNameWithoutExtension(path);
                    ComLate.Call(app, "RunMacro", path, module + ".main");
                    return new MacroRunResult(true, null);
                }
                catch (Exception ex3)
                {
                    return new MacroRunResult(false, "Could not run macro: " + ex3.Message);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // temp file cleanup is best-effort
                }
            }
        }
    }

    /// <summary>
    /// Phase 1 plan executor: each CadOperation becomes one generated VBA macro run.
    /// (Phase 6 replaces the macro hop with native API calls through the same interface.)
    /// </summary>
    public sealed class SwMacroPlanExecutor : IPlanExecutor
    {
        private readonly ISwMacroRunner _runner;

        public SwMacroPlanExecutor(ISwMacroRunner runner)
        {
            if (runner == null)
            {
                throw new ArgumentNullException("runner");
            }
            _runner = runner;
        }

        public bool IsMutating(CadOperation op)
        {
            // Every Phase 1 operation changes the model.
            return true;
        }

        public OpResult Execute(CadOperation op)
        {
            MacroScript macro;
            try
            {
                macro = SwMacroGenerator.Generate(op);
            }
            catch (NotSupportedException ex)
            {
                return OpResult.Fail(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return OpResult.Fail(ex.Message);
            }

            var result = _runner.Run(macro);
            return result.Ok
                ? OpResult.Success()
                : OpResult.Fail(result.Error);
        }
    }
}
