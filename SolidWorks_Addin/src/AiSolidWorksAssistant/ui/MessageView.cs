using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Ai.SolidWorksAssistant.Chat;

namespace Ai.SolidWorksAssistant.Ui
{
    /// <summary>
    /// One chat bubble. Custom-painted rounded rectangle with wrapped text and
    /// RTL support for Persian messages. Docked Top inside the FlowLayoutPanel.
    /// </summary>
    public sealed class MessageView : Control
    {
        private const int HMargin = 10;
        private const int VPad = 8;
        private const int RoleTagHeight = 16;

        private readonly ChatMessage _message;
        private readonly UITexts _texts;

        public MessageView(ChatMessage message, UITexts texts)
        {
            _message = message;
            _texts = texts;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Theme.WindowBack;
            Dock = DockStyle.Top;
            Font = Theme.BaseFont();

            UpdateHeight();
        }

        private Color BubbleColor
        {
            get
            {
                switch (_message.Role)
                {
                    case ChatRole.User:
                        return Theme.UserBubble;
                    case ChatRole.Assistant:
                        return Theme.AssistantBubble;
                    default:
                        return Theme.SystemBubble;
                }
            }
        }

        private Color TextColor
        {
            get
            {
                switch (_message.Role)
                {
                    case ChatRole.User:
                        return Theme.UserText;
                    case ChatRole.Assistant:
                        return Theme.AssistantText;
                    default:
                        return Theme.SystemText;
                }
            }
        }

        private string RoleName
        {
            get
            {
                switch (_message.Role)
                {
                    case ChatRole.User:
                        return _texts.You;
                    case ChatRole.Assistant:
                        return _texts.Assistant;
                    default:
                        return _texts.System;
                }
            }
        }

        private TextFormatFlags TextFlags
        {
            get
            {
                var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
                if (_message.IsRtl)
                {
                    flags |= TextFormatFlags.RtlReading;
                }
                return flags;
            }
        }

        private ContentAlignment RoleTagAlignment
        {
            get
            {
                if (_message.IsRtl)
                {
                    return ContentAlignment.TopRight;
                }
                return ContentAlignment.TopLeft;
            }
        }

        public void UpdateHeight()
        {
            var availableWidth = Math.Max(60, Width - (HMargin * 2) - (VPad * 2));
            var textSize = TextRenderer.MeasureText(
                _message.Text,
                Font,
                new Size(availableWidth, int.MaxValue),
                TextFlags);
            Height = textSize.Height + (VPad * 2) + RoleTagHeight + 6;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateHeight();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bubble = new Rectangle(HMargin, RoleTagHeight, Width - (HMargin * 2), Height - RoleTagHeight - 4);
            if (bubble.Width <= 0 || bubble.Height <= 0)
            {
                return;
            }

            using (var path = RoundedRect(bubble, 10))
            {
                using (var brush = new SolidBrush(BubbleColor))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(Theme.Border, 1f))
                {
                    if (_message.Role != ChatRole.User)
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            var roleTagColor = _message.Role == ChatRole.User
                ? Theme.UserBubble
                : Color.FromArgb(110, 118, 128);

            TextRenderer.DrawText(
                g,
                RoleName + "  ·  " + _message.Timestamp.ToString("HH:mm"),
                Theme.SmallFont(),
                new Rectangle(HMargin, 0, Width - (HMargin * 2), RoleTagHeight),
                roleTagColor,
                _message.IsRtl ? ContentAlignment.TopRight : ContentAlignment.TopLeft);

            var textRect = new Rectangle(
                bubble.X + VPad,
                bubble.Y + VPad - 1,
                bubble.Width - (VPad * 2),
                bubble.Height - (VPad * 2));

            TextRenderer.DrawText(g, _message.Text, Font, textRect, TextColor, TextFlags);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0)
            {
                path.AddRectangle(new Rectangle(r.X, r.Y, 1, 1));
                return path;
            }
            var d = Math.Min(radius, Math.Min(r.Width, r.Height) / 2) * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
