using System;
using System.Collections.Generic;

namespace Ai.SolidWorksAssistant.Core.ModelContext
{
    /// <summary>One row of the feature tree summary.</summary>
    public sealed class FeatureSummary
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
    }

    /// <summary>
    /// A safe, read-only snapshot of the current SOLIDWORKS model state.
    /// The AI layer sees only this — never raw COM objects (architecture rule).
    /// </summary>
    public sealed class ModelSnapshot
    {
        public bool IsConnected { get; set; }
        public bool HasActiveDocument { get; set; }
        public bool ReadSucceeded { get; set; }
        public string Detail { get; set; }

        public string DocumentType { get; set; }
        public string DocumentTitle { get; set; }
        public string DocumentPath { get; set; }
        public string ConfigurationName { get; set; }
        public int? ConfigurationCount { get; set; }
        public int? SelectedObjectCount { get; set; }
        public bool? SketchActive { get; set; }

        public List<FeatureSummary> Features { get; set; }

        public ModelSnapshot()
        {
            Features = new List<FeatureSummary>();
        }
    }

    public interface IModelContextReader
    {
        ModelSnapshot Read();
    }
}
