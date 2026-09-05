using System.Linq;
using Ai.SolidWorksAssistant.Cad;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class PlanPipelineTests
    {
        private static (CadPlan plan, ParseResult parsed) ParseSample()
        {
            var parsed = PlanParser.Parse(TestPlans.SampleBracket);
            Assert.False(parsed.HasErrors);
            return (parsed.Plan, parsed);
        }

        [Fact]
        public void Executes_all_operations_on_approval()
        {
            var tuple = ParseSample();
            var executor = new FakeExecutor();
            var interactor = new ScriptedInteractor { Approve = true };

            var outcome = PlanPipeline.Execute(tuple.plan, executor, interactor);

            Assert.Equal(PlanStatus.Succeeded, outcome.Status);
            Assert.Equal(3, outcome.CompletedOperations);
            Assert.Equal(3, outcome.TotalOperations);
            Assert.Equal(3, executor.Executed.Count);
            Assert.Contains(interactor.Reports, r => r.Contains("3/3"));
        }

        [Fact]
        public void Rejection_cancels_without_executing_anything()
        {
            var tuple = ParseSample();
            var executor = new FakeExecutor();
            var interactor = new ScriptedInteractor { Approve = false };

            var outcome = PlanPipeline.Execute(tuple.plan, executor, interactor);

            Assert.Equal(PlanStatus.Cancelled, outcome.Status);
            Assert.Empty(executor.Executed);
        }

        [Fact]
        public void Midway_failure_stops_the_plan()
        {
            var tuple = ParseSample();
            var executor = new FakeExecutor
            {
                Behavior = op => op.Type == OpTypes.FeatureExtrude
                    ? OpResult.Fail("Extrude failed.")
                    : OpResult.Success()
            };
            var interactor = new ScriptedInteractor();

            var outcome = PlanPipeline.Execute(tuple.plan, executor, interactor);

            Assert.Equal(PlanStatus.Failed, outcome.Status);
            Assert.Equal(1, outcome.CompletedOperations);
            Assert.Equal("Extrude failed.", outcome.Error);
            Assert.Equal(2, executor.Executed.Count); // rectangle + failed extrude
            Assert.Contains(interactor.Reports, r => r.Contains("failed"));
        }

        [Fact]
        public void Executor_exception_is_caught_and_reported()
        {
            var tuple = ParseSample();
            var executor = new FakeExecutor
            {
                Behavior = op => { throw new InvalidOperationException("boom"); }
            };
            var interactor = new ScriptedInteractor();

            var outcome = PlanPipeline.Execute(tuple.plan, executor, interactor);

            Assert.Equal(PlanStatus.Failed, outcome.Status);
            Assert.Contains("boom", outcome.Error);
        }

        [Fact]
        public void Progress_reports_flow_to_the_interactor()
        {
            var tuple = ParseSample();
            var executor = new FakeExecutor();
            var interactor = new ScriptedInteractor();

            PlanPipeline.Execute(tuple.plan, executor, interactor);

            Assert.Contains(interactor.Kinds, k => k == "Progress");
            Assert.Contains(interactor.Reports, r => r.Contains("1/3"));
            Assert.Contains(interactor.Reports, r => r.Contains("All 3 operations executed"));
        }
    }
}
