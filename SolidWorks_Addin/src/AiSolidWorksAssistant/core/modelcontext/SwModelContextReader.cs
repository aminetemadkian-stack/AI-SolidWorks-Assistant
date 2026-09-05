using System;
using System.Collections.Generic;
using Ai.SolidWorksAssistant.Addin;

namespace Ai.SolidWorksAssistant.Core.ModelContext
{
    /// <summary>
    /// Reads a read-only summary of the active SOLIDWORKS document via IDispatch
    /// late binding. Every call is guarded — reading context must never throw into
    /// the caller or disturb SOLIDWORKS.
    /// </summary>
    public sealed class SwModelContextReader : IModelContextReader
    {
        // Feature tree walk is bounded so a pathological model cannot hang the pane.
        private const int MaxFeatures = 500;

        private readonly Func<object> _swAppProvider;

        public SwModelContextReader(Func<object> swAppProvider)
        {
            if (swAppProvider == null)
            {
                throw new ArgumentNullException("swAppProvider");
            }
            _swAppProvider = swAppProvider;
        }

        public ModelSnapshot Read()
        {
            var snap = new ModelSnapshot();

            object app;
            try
            {
                app = _swAppProvider();
            }
            catch (Exception ex)
            {
                snap.Detail = "SOLIDWORKS application not available: " + ex.Message;
                return snap;
            }

            if (app == null)
            {
                snap.Detail = "Not connected to SOLIDWORKS.";
                return snap;
            }

            snap.IsConnected = true;

            try
            {
                var doc = ComLate.TryGet(app, "ActiveDoc");
                if (doc == null)
                {
                    snap.HasActiveDocument = false;
                    snap.ReadSucceeded = true;
                    return snap;
                }

                snap.HasActiveDocument = true;

                var typeCode = ComLate.TryInt(doc, "GetType");
                snap.DocumentType = DocumentTypeName(typeCode);
                snap.DocumentTitle = ComLate.TryString(doc, "GetTitle");
                snap.DocumentPath = ComLate.TryString(doc, "GetPathName");

                var cfg = ComLate.TryCall(doc, "GetActiveConfiguration");
                if (cfg != null)
                {
                    snap.ConfigurationName = ComLate.TryString(cfg, "Name");
                }

                var cfgCount = ComLate.TryInt(doc, "GetConfigurationCount");
                if (cfgCount.HasValue)
                {
                    snap.ConfigurationCount = cfgCount.Value;
                }

                var selMgr = ComLate.TryGet(doc, "SelectionManager");
                if (selMgr != null)
                {
                    var selCount = ComLate.TryCall(selMgr, "GetSelectedObjectCount2", -1);
                    if (selCount != null)
                    {
                        snap.SelectedObjectCount = Convert.ToInt32(selCount);
                    }
                }

                var activeSketch = ComLate.TryGet(ComLate.TryGet(doc, "SketchManager") ?? doc, "ActiveSketch");
                snap.SketchActive = activeSketch != null;

                ReadFeatures(doc, snap);

                snap.ReadSucceeded = true;
            }
            catch (Exception ex)
            {
                snap.ReadSucceeded = false;
                snap.Detail = ex.Message;
            }

            return snap;
        }

        private static void ReadFeatures(object doc, ModelSnapshot snap)
        {
            try
            {
                var features = new List<FeatureSummary>();
                var current = ComLate.TryCall(doc, "FirstFeature");
                var guard = 0;

                while (current != null && guard++ < MaxFeatures)
                {
                    var name = ComLate.TryString(current, "Name");
                    var typeName = ComLate.TryString(current, "GetTypeName2");
                    features.Add(new FeatureSummary
                    {
                        Name = name ?? "?",
                        TypeName = typeName ?? "?"
                    });
                    current = ComLate.TryCall(current, "GetNextFeature");
                }

                snap.Features = features;
            }
            catch
            {
                // Feature details are optional; keep the rest of the snapshot.
            }
        }

        private static string DocumentTypeName(int? typeCode)
        {
            if (!typeCode.HasValue)
            {
                return null;
            }

            switch (typeCode.Value)
            {
                case SwConst.swDocPART:
                    return "Part";
                case SwConst.swDocASSEMBLY:
                    return "Assembly";
                case SwConst.swDocDRAWING:
                    return "Drawing";
                default:
                    return "Unknown(" + typeCode.Value + ")";
            }
        }
    }
}
