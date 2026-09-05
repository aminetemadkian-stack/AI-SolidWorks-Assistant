using System.Drawing;
using System.Windows.Forms;

namespace Ai.SolidWorksAssistant.Ui
{
    /// <summary>Central look & feel for the chat pane.</summary>
    public static class Theme
    {
        public static readonly Color WindowBack = Color.FromArgb(245, 247, 250);
        public static readonly Color HeaderBack = Color.FromArgb(0, 114, 200);
        public static readonly Color HeaderText = Color.White;
        public static readonly Color UserBubble = Color.FromArgb(0, 114, 200);
        public static readonly Color UserText = Color.White;
        public static readonly Color AssistantBubble = Color.White;
        public static readonly Color AssistantText = Color.FromArgb(30, 34, 40);
        public static readonly Color SystemBubble = Color.FromArgb(228, 233, 240);
        public static readonly Color SystemText = Color.FromArgb(70, 76, 84);
        public static readonly Color Border = Color.FromArgb(214, 220, 229);
        public static readonly Color ApproveGreen = Color.FromArgb(46, 160, 67);
        public static readonly Color RejectRed = Color.FromArgb(180, 48, 44);

        public static Font BaseFont()
        {
            // Segoe UI has full Persian/Arabic glyph coverage on Windows.
            return new Font("Segoe UI", 9f, FontStyle.Regular);
        }

        public static Font TitleFont()
        {
            return new Font("Segoe UI", 10f, FontStyle.Bold);
        }

        public static Font SmallFont()
        {
            return new Font("Segoe UI", 8f, FontStyle.Regular);
        }

        public static void Apply(Control control)
        {
            control.Font = BaseFont();
        }
    }
}
