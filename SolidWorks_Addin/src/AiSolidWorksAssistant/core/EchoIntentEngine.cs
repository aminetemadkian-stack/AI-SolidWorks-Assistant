using System;
using System.Collections.Generic;
using System.IO;
using Ai.SolidWorksAssistant.Core.ModelContext;

namespace Ai.SolidWorksAssistant.Core
{
    public sealed class EngineResponse
    {
        public string ReplyText { get; set; }
        public string ProposedPlanMarkdown { get; set; }
        public IDictionary<string, string> Metadata { get; set; }

        public EngineResponse()
        {
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// The intent engine contract. Phase 1 ships an "echo" engine; Phase 2 adds the
    /// AI Core controller and Phase 3-4 the online/offline providers. Engines never
    /// touch SOLIDWORKS — they only return text + a proposed plan (architecture rule).
    /// </summary>
    public interface IIntentEngine
    {
        EngineResponse Process(string userText, ModelSnapshot context);
    }

    /// <summary>
    /// Phase 1 demo engine: acknowledges the request and proposes the built-in
    /// sample plan. It NEVER executes anything by itself — the plan only runs after
    /// the user explicitly approves it in the confirmation dialog.
    /// </summary>
    public sealed class EchoIntentEngine : IIntentEngine
    {
        public EngineResponse Process(string userText, ModelSnapshot context)
        {
            var goal = (userText ?? string.Empty).Trim();
            if (goal.Length > 160)
            {
                goal = goal.Substring(0, 157) + "...";
            }

            return new EngineResponse
            {
                ReplyText = goal,
                ProposedPlanMarkdown = SamplePlanLoader.LoadDefaultPlan(),
                Metadata =
                {
                    { "engine", "echo" },
                    { "mode", "phase1-demo" }
                }
            };
        }
    }

    /// <summary>Loads the embedded sample plan used by the Phase 1 echo engine.</summary>
    public static class SamplePlanLoader
    {
        private const string ResourceName = "Ai.SolidWorksAssistant.samples.plan.md";

        public static string LoadDefaultPlan()
        {
            var assembly = typeof(SamplePlanLoader).Assembly;
            using (var stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        "Embedded sample plan '" + ResourceName + "' not found. Resources: " +
                        string.Join(", ", assembly.GetManifestResourceNames()));
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
