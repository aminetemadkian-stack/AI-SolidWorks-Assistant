using System;
using System.IO;
using System.Runtime.InteropServices;
using Ai.SolidWorksAssistant.Cad;
using Ai.SolidWorksAssistant.Chat;
using Ai.SolidWorksAssistant.Core;
using Ai.SolidWorksAssistant.Core.ModelContext;
using Ai.SolidWorksAssistant.Settings;
using Ai.SolidWorksAssistant.Ui;

namespace Ai.SolidWorksAssistant.Addin
{
    /// <summary>
    /// Phase 1 add-in entry point.
    ///
    /// Registers with SOLIDWORKS through:
    ///   HKCU\SOFTWARE\SolidWorks\Addins\{597151FC-6387-4AEE-B0F7-E0844B6E9F0F}
    /// SOLIDWORKS CoCreates this class, QIs ISwAddin and calls ConnectToSW.
    ///
    /// Phase 1 scope (see docs/fa/architecture.md):
    ///   - Task Pane chat UI (RTL/LTR)
    ///   - Model context reader (active document summary)
    ///   - Echo intent engine + PlanScript parser/validator
    ///   - Plan pipeline executing validated CAD operations via generated VBA macros
    /// </summary>
    [ComVisible(true)]
    [Guid("597151FC-6387-4AEE-B0F7-E0844B6E9F0F")]
    [ProgId("Ai.SolidWorksAssistant.SwAddinMain")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public sealed class SwAddinMain : ISwAddin
    {
        public const string AddInTitle = "AI SolidWorks Assistant";
        public const string AddInDescription =
            "AI assistant foundation — chat task pane, model context and a validated CAD command pipeline (Phase 1).";

        private object _swApp;              // SOLIDWORKS application object (late-bound)
        private int _cookie;
        private object _commandManager;     // ICommandManager (late-bound)
        private object _taskPaneView;       // ITaskpaneView (late-bound)
        private TaskPaneHostControl _host;
        private ChatViewModel _viewModel;
        private AppSettings _settings;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            _swApp = ThisSW;
            _cookie = Cookie;

            try
            {
                Log.Initialize();
                Log.Info("ConnectToSW: cookie=" + Cookie);
            }
            catch
            {
                // Logging must never block add-in load.
            }

            try
            {
                _settings = new JsonSettingsStore().Load();
            }
            catch
            {
                _settings = new AppSettings();
            }

            var texts = UITexts.Resolve(_settings.Language);

            // Register this add-in as the callback target for CommandManager buttons.
            TryRegisterCallbackInfo();

            var paneCreated = TryCreateTaskPane(texts);
            TryCreateCommandManagerUi(texts); // best-effort; task pane is the critical piece

            Log.Info("ConnectToSW finished. TaskPane=" + (paneCreated ? "OK" : "FAILED"));
            return true; // always keep the add-in loaded; errors are logged
        }

        public bool DisconnectFromSW()
        {
            Log.Info("DisconnectFromSW");
            try
            {
                if (_taskPaneView != null)
                {
                    ComLate.TryCall(_taskPaneView, "DeleteView");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("DeleteView failed: " + ex.Message);
            }

            try
            {
                if (_commandManager != null)
                {
                    ComLate.TryCall(_commandManager, "RemoveCommandGroup", SwConst.CommandGroupId);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("RemoveCommandGroup failed: " + ex.Message);
            }

            if (_viewModel != null)
            {
                _viewModel.Shutdown();
                _viewModel = null;
            }

            _taskPaneView = null;
            _commandManager = null;
            _host = null;
            return true;
        }

        /// <summary>CommandManager button callback — shows the AI chat task pane again after the user closed it.</summary>
        public void ShowTaskPane()
        {
            try
            {
                if (_taskPaneView != null)
                {
                    ComLate.TryCall(_taskPaneView, "Show");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("ShowTaskPane failed: " + ex.Message);
            }
        }

        [ComVisible(true)]
        public void ShowTaskPaneUpdate(ref int retval)
        {
            // Enable-callback for the CommandManager button: always enabled.
            retval = 1;
        }

        private void TryRegisterCallbackInfo()
        {
            try
            {
                ComLate.TryCall(_swApp, "SetAddinCallbackInfo2", 0, this, _cookie);
            }
            catch (Exception ex)
            {
                Log.Warn("SetAddinCallbackInfo2 failed: " + ex.Message);
            }
        }

        private bool TryCreateTaskPane(UITexts texts)
        {
            try
            {
                _commandManager = ComLate.Call(_swApp, "GetCommandManager", _cookie);

                _host = new TaskPaneHostControl(BuildChatSurface);
                _host.Dock = System.Windows.Forms.DockStyle.Fill;

                var iconsDir = Path.Combine(GetAssemblyDirectory(), "Resources", "icons");
                var icon16 = Path.Combine(iconsDir, "taskpane_16.bmp");
                var icon24 = Path.Combine(iconsDir, "taskpane_24.bmp");
                var icon32 = Path.Combine(iconsDir, "taskpane_32.bmp");

                // Preferred: high-res icon list (SOLIDWORKS 2017+)
                _taskPaneView = ComLate.TryCall(
                    _commandManager, "CreateTaskpaneView3",
                    new object[] { icon16, icon24, icon32 }, texts.TaskPaneTitle);

                if (_taskPaneView == null)
                {
                    // Fallback: single bitmap overload
                    _taskPaneView = ComLate.TryCall(
                        _commandManager, "CreateTaskpaneView2", icon16, texts.TaskPaneTitle);
                }

                if (_taskPaneView == null)
                {
                    Log.Error("Could not create the task pane view.");
                    return false;
                }

                ComLate.Call(_taskPaneView, "DisplayControlFromControl", _host);
                Log.Info("Task pane created and chat surface attached.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Task pane creation failed", ex);
                return false;
            }
        }

        private void TryCreateCommandManagerUi(UITexts texts)
        {
            // Best-effort toolbar/tab so the user can re-open the pane after closing it.
            // Any failure here is logged and ignored — the add-in stays fully usable.
            try
            {
                var errorPlaceholder = 0;
                var cmdGroup = ComLate.CallWithByRef(
                    _commandManager,
                    "CreateCommandGroup2",
                    new object[]
                    {
                        SwConst.CommandGroupId,
                        AddInTitle,
                        AddInTitle,
                        AddInDescription,
                        -1,
                        errorPlaceholder
                    },
                    new[] { 5 });

                if (cmdGroup == null)
                {
                    Log.Warn("CreateCommandGroup2 returned null; toolbar UI skipped.");
                    return;
                }

                var iconsDir = Path.Combine(GetAssemblyDirectory(), "Resources", "icons");
                var cmdIdObj = ComLate.Call(
                    cmdGroup,
                    "AddCommandItem2",
                    texts.TaskPaneTitle,       // Name
                    -1,                        // Position (auto)
                    texts.TaskPaneTitle,       // HintString
                    texts.TaskPaneTitle,       // ToolTip
                    0,                         // ImageListIndex
                    "ShowTaskPane",            // CallbackFunction (click handler)
                    "ShowTaskPaneUpdate",      // EnableMethod (update callback)
                    0,                         // UserID
                    0);                        // MenuTBOption (swDefaultButton)
                var cmdId = Convert.ToInt32(cmdIdObj);

                ComLate.Set(cmdGroup, "SmallIconList", new object[] { Path.Combine(iconsDir, "command_16.bmp") });
                ComLate.Set(cmdGroup, "LargeIconList", new object[] { Path.Combine(iconsDir, "command_24.bmp") });
                ComLate.Set(cmdGroup, "HasToolbar", true);
                ComLate.Set(cmdGroup, "HasMenu", true);
                ComLate.Set(cmdGroup, "Active", true);

                int[] docTypes = { SwConst.swDocPART, SwConst.swDocASSEMBLY, SwConst.swDocDRAWING };
                foreach (var docType in docTypes)
                {
                    try
                    {
                        var tab = ComLate.TryCall(_commandManager, "GetCommandTab", docType, AddInTitle);
                        if (tab == null)
                        {
                            tab = ComLate.TryCall(_commandManager, "AddCommandTab", docType, AddInTitle);
                        }
                        if (tab == null)
                        {
                            continue;
                        }

                        var box = ComLate.TryCall(tab, "AddCommandTabBox");
                        if (box == null)
                        {
                            continue;
                        }

                        ComLate.TryCall(
                            box, "AddCommands",
                            new object[] { cmdId },
                            new object[] { SwConst.swDefaultButton });
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("Command tab setup failed for docType=" + docType + ": " + ex.Message);
                    }
                }

                Log.Info("CommandManager UI created.");
            }
            catch (Exception ex)
            {
                Log.Warn("CommandManager UI creation failed (non-fatal): " + ex.Message);
            }
        }

        private System.Windows.Forms.Control BuildChatSurface()
        {
            var texts = UITexts.Resolve(_settings != null ? _settings.Language : "auto");

            var contextReader = new SwModelContextReader(() => _swApp);
            var runner = new SwMacroRunner(() => _swApp);
            var executor = new SwMacroPlanExecutor(runner);
            var engine = new Core.EchoIntentEngine();

            _viewModel = new ChatViewModel(engine, contextReader, executor, texts);
            return new ChatView(_viewModel, texts);
        }

        private static string GetAssemblyDirectory()
        {
            var assembly = typeof(SwAddinMain).Assembly;
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location))
            {
                location = new Uri(assembly.CodeBase).LocalPath;
            }
            return Path.GetDirectoryName(location);
        }
    }
}
