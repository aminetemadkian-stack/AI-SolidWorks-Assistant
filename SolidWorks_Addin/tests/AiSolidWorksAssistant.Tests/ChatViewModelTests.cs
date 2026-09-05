using System.Linq;
using System.Threading;
using Ai.SolidWorksAssistant.Cad;
using Ai.SolidWorksAssistant.Chat;
using Ai.SolidWorksAssistant.Core;
using Ai.SolidWorksAssistant.Core.ModelContext;
using Ai.SolidWorksAssistant.Ui;
using Xunit;

namespace Ai.SolidWorksAssistant.Tests
{
    public class ChatViewModelTests
    {
        private static UITexts T()
        {
            return UITexts.EnglishTexts();
        }

        private static ChatViewModel CreateVm(
            FakeExecutor executor,
            IModelContextReader reader = null,
            string plan = TestPlans.SampleBracket)
        {
            var engine = new StubEngine(plan);
            return new ChatViewModel(engine, reader, executor, T())
            {
                AutoApprove = true
            };
        }

        private sealed class StubEngine : IIntentEngine
        {
            private readonly string _plan;
            public StubEngine(string plan) { _plan = plan; }

            public EngineResponse Process(string userText, ModelSnapshot context)
            {
                return new EngineResponse
                {
                    ReplyText = userText,
                    ProposedPlanMarkdown = _plan
                };
            }
        }

        private sealed class NullContextReader : IModelContextReader
        {
            public ModelSnapshot Read()
            {
                return new ModelSnapshot { IsConnected = false };
            }
        }

        [Fact]
        public void Full_flow_produces_user_assistant_and_outcome_messages()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor, new NullContextReader());

            vm.HandleUserInput("make a bracket");

            var roles = vm.Messages.Select(m => m.Role).ToList();
            Assert.Contains(ChatRole.User, roles);
            Assert.Contains(ChatRole.System, roles);   // context card + outcome
            Assert.Contains(ChatRole.Assistant, roles); // plan proposal
            Assert.Contains(vm.Messages.Last().Text, "3 of 3");
            Assert.Equal(3, executor.Executed.Count);
        }

        [Fact]
        public void Parse_failure_reports_error_and_executes_nothing()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor, plan: "plan: empty\nmeta\nx = 1");

            vm.HandleUserInput("test");

            Assert.Empty(executor.Executed);
            Assert.Contains(vm.Messages.Select(m => m.Text), t => t.Contains("failed validation"));
        }

        [Fact]
        public void Validation_failure_reports_error_and_executes_nothing()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor, plan: "operation feature.fillet\nradius_mm = 3");

            vm.HandleUserInput("test");

            Assert.Empty(executor.Executed);
            Assert.Contains(vm.Messages.Select(m => m.Text), t => t.Contains("Phase 6"));
        }

        [Fact]
        public void Failed_operation_is_reported_with_progress()
        {
            var executor = new FakeExecutor
            {
                Behavior = op => op.Type == OpTypes.FeatureExtrude
                    ? OpResult.Fail("Extrude failed.")
                    : OpResult.Success()
            };
            var vm = CreateVm(executor);

            vm.HandleUserInput("test");

            // rectangle succeeds, extrude fails → plan stops after 2 attempts
            Assert.Equal(2, executor.Executed.Count);
            Assert.Contains(vm.Messages.Select(m => m.Text), t => t.Contains("Extrude failed."));
        }

        [Fact]
        public void Confirmation_flow_blocks_and_resolves()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor);
            vm.AutoApprove = false;

            bool pendingSeen = false;
            vm.ConfirmationPendingChanged += pending => pendingSeen = pending;

            var worker = new System.Threading.Thread(() => vm.HandleUserInput("test"));
            worker.Start();
            worker.Join(500); // let it reach the confirmation gate

            Assert.True(pendingSeen);
            Assert.Empty(executor.Executed); // nothing ran before approval

            vm.ApprovePlan();
            Assert.True(worker.Join(2000)); // worker finished after approval
            Assert.Equal(3, executor.Executed.Count);
        }

        [Fact]
        public void Rejection_posts_cancelled_outcome()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor);
            vm.AutoApprove = false;

            var worker = new System.Threading.Thread(() => vm.HandleUserInput("test"));
            worker.Start();
            worker.Join(500);

            vm.RejectPlan();
            Assert.True(worker.Join(2000));

            Assert.Empty(executor.Executed);
            Assert.Contains(vm.Messages.Select(m => m.Text), t => t.Contains("cancelled"));
        }

        [Fact]
        public void Persian_input_is_detected_as_rtl()
        {
            var executor = new FakeExecutor();
            var vm = CreateVm(executor);

            vm.HandleUserInput("یک مستطیل بکش");

            var userMessage = vm.Messages.First(m => m.Role == ChatRole.User);
            Assert.True(userMessage.IsRtl);
        }

        [Fact]
        public void Context_summary_shows_active_document()
        {
            var executor = new FakeExecutor();
            var reader = new FixedReader(new ModelSnapshot
            {
                IsConnected = true,
                HasActiveDocument = true,
                DocumentTitle = "Bracket",
                DocumentType = "Part",
                ConfigurationName = "Default",
                Features =
                {
                    new FeatureSummary { Name = "Boss-Extrude1", TypeName = "Extrusion" }
                }
            });
            var vm = CreateVm(executor, reader);

            vm.HandleUserInput("test");

            var contextMessage = vm.Messages.First(m => m.Text.Contains("Bracket"));
            Assert.Contains("Part", contextMessage.Text);
            Assert.Contains("Boss-Extrude1", contextMessage.Text);
        }

        private sealed class FixedReader : IModelContextReader
        {
            private readonly ModelSnapshot _snapshot;
            public FixedReader(ModelSnapshot snapshot) { _snapshot = snapshot; }
            public ModelSnapshot Read() { return _snapshot; }
        }
    }
}
