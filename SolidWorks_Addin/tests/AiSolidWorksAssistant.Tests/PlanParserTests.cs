using System.Linq;
using Ai.SolidWorksAssistant.Cad;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class PlanParserTests
    {
        [Fact]
        public void Parses_sample_bracket_plan()
        {
            var result = PlanParser.Parse(TestPlans.SampleBracket);

            Assert.False(result.HasErrors);
            Assert.Equal(3, result.Plan.Operations.Count);
            Assert.Equal("Test bracket", result.Plan.Title);

            var rect = result.Plan.Operations[0];
            Assert.Equal(OpTypes.SketchRectangle, rect.Type);
            Assert.Equal(100.0, rect.Number("x2"));
            Assert.Equal(50.0, rect.Number("y2"));

            var extrude = result.Plan.Operations[1];
            Assert.Equal(OpTypes.FeatureExtrude, extrude.Type);
            Assert.Equal(10.0, extrude.Number("depth_mm"));

            var hole = result.Plan.Operations[2];
            Assert.Equal(OpTypes.FeatureHole, hole.Type);
            Assert.Equal(10.0, hole.Number("diameter_mm"));
            Assert.Equal(true, hole.Flag("through"));
        }

        [Fact]
        public void Normalizes_persian_digits_and_separators()
        {
            var plan = "plan: t\n\noperation sketch.rectangle\nx1 = ۰\ny1 = 0\nx2 = ۱٫۵\ny2 = ۵۰";
            var result = PlanParser.Parse(plan);

            Assert.False(result.HasErrors);
            var rect = result.Plan.Operations[0];
            Assert.Equal(0.0, rect.Number("x1"));
            Assert.Equal(1.5, rect.Number("x2"));
            Assert.Equal(50.0, rect.Number("y2"));
        }

        [Fact]
        public void Accepts_value_units_and_markdown_noise()
        {
            var plan = "# header\n```text\nplan: noisy\n---\n> a quote\noperation feature.extrude\ndepth_mm = 10 mm\n// trailing comment";
            var result = PlanParser.Parse(plan);

            Assert.False(result.HasErrors);
            Assert.Equal(1, result.Plan.Operations.Count);
            Assert.Equal(10.0, result.Plan.Operations[0].Number("depth_mm"));
        }

        [Fact]
        public void Colon_separator_is_accepted()
        {
            var plan = "operation feature.hole\nx: 12\ny: 30\ndiameter_mm: 6";
            var result = PlanParser.Parse(plan);

            Assert.False(result.HasErrors);
            var hole = result.Plan.Operations[0];
            Assert.Equal(12.0, hole.Number("x"));
            Assert.Equal(30.0, hole.Number("y"));
            Assert.Equal(6.0, hole.Number("diameter_mm"));
        }

        [Fact]
        public void Empty_plan_is_an_error()
        {
            var result = PlanParser.Parse("   \n# only a comment\n");
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Plan_without_operations_is_an_error()
        {
            var result = PlanParser.Parse("plan: nothing here\nmeta\nfoo = bar");
            Assert.True(result.HasErrors);
            Assert.Empty(result.Plan.Operations);
        }

        [Fact]
        public void Unknown_line_produces_warning_not_error()
        {
            var result = PlanParser.Parse(TestPlans.SampleBracket + "\nsome random line");
            Assert.False(result.HasErrors);
            Assert.Contains(result.Warnings, w => w.Contains("random"));
        }

        [Fact]
        public void Describe_localizes_operation_summaries()
        {
            var parsed = PlanParser.Parse(TestPlans.SampleBracket);
            Assert.Equal(3, parsed.Plan.Operations.Count);

            var en = PlanParser.Describe(parsed.Plan.Operations[2], false);
            Assert.Contains("Hole", en);
            Assert.Contains("through all", en);

            var fa = PlanParser.Describe(parsed.Plan.Operations[2], true);
            Assert.Contains("سوراخ", fa);
            Assert.Contains("عبور از تمام", fa);
        }
    }
}
