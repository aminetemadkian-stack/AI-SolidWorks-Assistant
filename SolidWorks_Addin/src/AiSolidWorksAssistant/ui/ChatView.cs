using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ai.SolidWorksAssistant.Chat;

namespace Ai.SolidWorksAssistant.Ui
{
    /// <summary>
    /// The chat surface hosted in the SOLIDWORKS task pane.
    /// Wires ChatViewModel events onto WinForms controls with proper thread marshaling
    /// (model events arrive on background threads) and per-message RTL handling.
    /// </summary>
    public sealed class ChatView : UserControl
    {
        private readonly ChatViewModel _vm;
        private readonly UITexts _t;

        private Panel _header;
        private Label _titleLabel;
        private Label _statusLabel;
        private FlowLayoutPanel _messages;
        private Panel _inputPanel;
        private TextBox _input;
        private Button _sendButton;
        private Button _samplesButton;
        private Panel _confirmBar;
        private Button _approveButton;
        private Button _rejectButton;

        public ChatView(ChatViewModel viewModel, UITexts texts)
        {
            _vm = viewModel;
            _t = texts;

            Dock = DockStyle.Fill;
            BackColor = Theme.WindowBack;
            Font = Theme.BaseFont();

            BuildControls();

            _vm.MessageAdded += OnMessageAdded;
            _vm.BusyChanged += OnBusyChanged;
            _vm.ConfirmationPendingChanged += OnConfirmationPendingChanged;
            _vm.StatusChanged += OnStatusChanged;

            OnStatusChanged(_t.Ready);
            SafeBeginInvoke(() => _vm.PostWelcome());
        }

        private void BuildControls()
        {
            SuspendLayout();

            // ---- header ----
            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Theme.HeaderBack
            };
            _titleLabel = new Label
            {
                Text = _t.ChatTitle,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Theme.HeaderText,
                Font = Theme.TitleFont(),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusLabel = new Label
            {
                Text = _t.Ready,
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(205, 226, 245),
                Font = Theme.SmallFont(),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _header.Controls.Add(_statusLabel);
            _header.Controls.Add(_titleLabel);

            // ---- confirmation bar (hidden until a plan awaits approval) ----
            _confirmBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(255, 246, 225),
                Visible = false
            };
            _approveButton = new Button
            {
                Text = "✔ " + _t.Approve,
                Width = 120,
                Height = 28,
                Location = new Point(8, 6),
                BackColor = Theme.ApproveGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _approveButton.FlatAppearance.BorderSize = 0;
            _approveButton.Click += (s, e) => _vm.ApprovePlan();

            _rejectButton = new Button
            {
                Text = "✖ " + _t.Reject,
                Width = 90,
                Height = 28,
                Location = new Point(136, 6),
                BackColor = Theme.RejectRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _rejectButton.FlatAppearance.BorderSize = 0;
            _rejectButton.Click += (s, e) => _vm.RejectPlan();

            _confirmBar.Controls.Add(_approveButton);
            _confirmBar.Controls.Add(_rejectButton);

            // ---- input bar ----
            _inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = Theme.WindowBack,
                Padding = new Padding(6)
            };
            _input = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            _input.TextChanged += (s, e) => UpdateInputRtl();
            _input.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SendCurrent();
                }
            };

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 168,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0)
            };
            _sendButton = new Button
            {
                Text = _t.Send,
                Width = 160,
                Height = 30
            };
            _sendButton.Click += (s, e) => SendCurrent();

            _samplesButton = new Button
            {
                Text = _t.Samples,
                Width = 160,
                Height = 26
            };
            _samplesButton.Click += (s, e) =>
            {
                _input.Text = _t.Persian ? _t.SampleCommandFa : _t.SampleCommandEn;
                _input.Focus();
            };

            buttonsPanel.Controls.Add(_sendButton);
            buttonsPanel.Controls.Add(_samplesButton);

            _inputPanel.Controls.Add(_input);
            _inputPanel.Controls.Add(buttonsPanel);

            // ---- messages ----
            _messages = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Theme.WindowBack,
                Padding = new Padding(2)
            };

            Controls.Add(_messages);
            Controls.Add(_confirmBar);
            Controls.Add(_inputPanel);
            Controls.Add(_header);

            Resize += (s, e) => StretchMessages();

            ResumeLayout();
        }

        private void StretchMessages()
        {
            var width = _messages.ClientSize.Width - 8;
            if (width < 60)
            {
                return;
            }
            foreach (Control child in _messages.Controls)
            {
                child.Width = width;
                var bubble = child as MessageView;
                if (bubble != null)
                {
                    bubble.UpdateHeight();
                }
            }
        }

        private void UpdateInputRtl()
        {
            // Live RTL: align the input box with the script being typed.
            var rtl = false;
            foreach (var ch in _input.Text)
            {
                var c = (int)ch;
                if (c >= 0x0600 && c <= 0x06FF)
                {
                    rtl = true;
                    break;
                }
            }
            var desired = rtl ? RightToLeft.Yes : RightToLeft.No;
            if (_input.RightToLeft != desired)
            {
                _input.RightToLeft = desired;
            }
        }

        private void SendCurrent()
        {
            var text = _input.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            _input.Clear();
            var vm = _vm;
            Task.Run(() => vm.HandleUserInput(text));
        }

        private void SafeBeginInvoke(Action action)
        {
            try
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(action);
                }
                else
                {
                    HandleCreated += (s, e) => BeginInvoke(action);
                }
            }
            catch
            {
                // UI teardown race — ignore
            }
        }

        // ---- ViewModel event handlers (background thread → UI thread) ----

        private void OnMessageAdded(ChatMessage message)
        {
            SafeBeginInvoke(() =>
            {
                var view = new MessageView(message, _t);
                view.Width = Math.Max(60, _messages.ClientSize.Width - 8);
                _messages.Controls.Add(view);
                _messages.ScrollControlIntoView(view);
            });
        }

        private void OnBusyChanged(bool busy)
        {
            SafeBeginInvoke(() =>
            {
                _sendButton.Enabled = !busy;
                _samplesButton.Enabled = !busy;
                _input.Enabled = !busy;
            });
        }

        private void OnConfirmationPendingChanged(bool pending)
        {
            SafeBeginInvoke(() =>
            {
                _confirmBar.Visible = pending;
            });
        }

        private void OnStatusChanged(string status)
        {
            SafeBeginInvoke(() =>
            {
                _statusLabel.Text = status;
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _vm != null)
            {
                _vm.MessageAdded -= OnMessageAdded;
                _vm.BusyChanged -= OnBusyChanged;
                _vm.ConfirmationPendingChanged -= OnConfirmationPendingChanged;
                _vm.StatusChanged -= OnStatusChanged;
            }
            base.Dispose(disposing);
        }
    }
}
