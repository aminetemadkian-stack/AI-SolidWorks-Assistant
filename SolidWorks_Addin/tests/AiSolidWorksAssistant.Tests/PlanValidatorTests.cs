using System.Linq;
using Ai.SolidWorksAssistant.Cad;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class PlanValidatorTests
    {
        [Fact]
        public void Valid_sample_plan_has_no_errors()
        {
            var parsed = PlanParser.Parse(TestPlans.SampleBracket);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.False(PlanValidator.HasErrors(issues), string.Join("; ", issues.Select(i => i.Message)));
        }

        [Fact]
        public void Phase6_operation_is_rejected()
        {
            var plan = "operation feature.fillet\nradius_mm = 3";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("Phase 6"));
        }

        [Fact]
        public void Unknown_operation_type_is_rejected()
        {
            var parsed = PlanParser.Parse("operation warp.drive\nspeed = 9");
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("Unknown operation type"));
        }

        [Fact]
        public void Non_positive_depth_is_rejected()
        {
            var plan = "operation sketch.rectangle\nx1 = 0\ny1 = 0\nx2 = 10\ny2 = 10\noperation feature.extrude\ndepth_mm = 0";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("greater than zero"));
        }

        [Fact]
        public void Missing_hole_position_is_rejected()
        {
            var plan = "operation feature.hole\ndiameter_mm = 10";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("'x'"));
            Assert.Contains(issues, i => i.Message.Contains("'y'"));
        }

        [Fact]
        public void Degenerate_rectangle_is_rejected()
        {
            var plan = "operation sketch.rectangle\nx1 = 5\ny1 = 5\nx2 = 5\ny2 = 5";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("degenerate"));
        }

        [Fact]
        public void Huge_dimension_is_rejected()
        {
            var plan = "operation sketch.rectangle\nx1 = 0\ny1 = 0\nx2 = 99999\ny2 = 10";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.True(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Message.Contains("sanity limit"));
        }

        [Fact]
        public void Extrude_without_sketch_warns_but_does_not_error()
        {
            var plan = "operation feature.extrude\ndepth_mm = 5";
            var parsed = PlanParser.Parse(plan);
            var issues = PlanValidator.Validate(parsed.Plan);

            Assert.False(PlanValidator.HasErrors(issues));
            Assert.Contains(issues, i => i.Severity == IssueSeverity.Warning);
        }
    }
}
