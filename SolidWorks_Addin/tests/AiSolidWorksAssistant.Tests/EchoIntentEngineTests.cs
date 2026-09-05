using Ai.SolidWorksAssistant.Cad;
using Ai.SolidWorksAssistant.Core;
using Ai.SolidWorksAssistant.Core.ModelContext;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class EchoIntentEngineTests
    {
        [Fact]
        public void Returns_a_parseable_plan_with_the_user_goal()
        {
            var engine = new EchoIntentEngine();
            var response = engine.Process("یک براکت ۱۰۰ در ۵۰ بساز", new ModelSnapshot());

            Assert.Contains("براکت", response.ReplyText);
            Assert.False(string.IsNullOrWhiteSpace(response.ProposedPlanMarkdown));
            Assert.Equal("echo", response.Metadata["engine"]);
        }

        [Fact]
        public void Proposed_plan_round_trips_through_parser_and_validator()
        {
            var engine = new EchoIntentEngine();
            var response = engine.Process("anything", null);

            var parsed = PlanParser.Parse(response.ProposedPlanMarkdown);
            Assert.False(parsed.HasErrors);

            var issues = PlanValidator.Validate(parsed.Plan);
            Assert.False(PlanValidator.HasErrors(issues));
        }

        [Fact]
        public void Embedded_sample_plan_resource_exists()
        {
            var markdown = SamplePlanLoader.LoadDefaultPlan();
            Assert.Contains("operation sketch.rectangle", markdown);
            Assert.Contains("operation feature.hole", markdown);
        }
    }
}
