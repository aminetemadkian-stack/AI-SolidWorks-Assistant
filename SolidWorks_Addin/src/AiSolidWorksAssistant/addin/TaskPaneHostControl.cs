using System;
using System.Windows.Forms;
using Ai.SolidWorksAssistant.Ui;

namespace Ai.SolidWorksAssistant.Addin
{
    /// <summary>
    /// Root control handed to SOLIDWORKS' ITaskpaneView.DisplayControlFromControl().
    /// Builds the real chat surface lazily via the provided factory so COM/UI setup
    /// failures can be logged without breaking the task pane itself.
    /// </summary>
    public sealed class TaskPaneHostControl : UserControl
    {
        private readonly Func<Control> _surfaceFactory;
        private bool _built;

        public TaskPaneHostControl(Func<Control> surfaceFactory)
        {
            if (surfaceFactory == null)
            {
                throw new ArgumentNullException("surfaceFactory");
            }
            _surfaceFactory = surfaceFactory;

            Dock = DockStyle.Fill;
            BackColor = Theme.WindowBack;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            BuildSurface();
        }

        private void BuildSurface()
        {
            if (_built)
            {
                return;
            }
            _built = true;

            try
            {
                var surface = _surfaceFactory();
                surface.Dock = DockStyle.Fill;
                Controls.Add(surface);
            }
            catch (Exception ex)
            {
                Core.Log.Error("Failed to build the chat surface", ex);
                Controls.Add(new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "AI Assistant UI failed to load.\r\n" + ex.Message,
                    ForeColor = Color.Firebrick,
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }
        }
    }
}
