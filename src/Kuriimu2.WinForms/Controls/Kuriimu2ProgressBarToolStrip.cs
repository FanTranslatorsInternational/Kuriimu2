using System.Drawing;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.Controls
{
    public class Kuriimu2ProgressBarToolStrip : ToolStripControlHost
    {
        protected Kuriimu2ProgressBar ProgressBar => (Kuriimu2ProgressBar)Control;

        public Kuriimu2ProgressBarToolStrip() : base(new Kuriimu2ProgressBar())
        {
        }

        public Kuriimu2ProgressBarToolStrip(string name) : base(new Kuriimu2ProgressBar(), name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the value of the progress bar.
        /// </summary>
        public virtual int Value
        {
            get => ProgressBar.Value;
            set => ProgressBar.Value = value;
        }

        /// <summary>
        /// Gets or sets the text shown on the progressbar
        /// </summary>
        public override string Text
        {
            get => ProgressBar.Text;
            set => ProgressBar.Text = value;
        }

        /// <summary>
        /// Gets or sets the text color of the progressbar
        /// </summary>
        public Color TextColor
        {
            get => ProgressBar.TextColor;
            set => ProgressBar.TextColor = value;
        }

        /// <summary>
        /// Gets or sets the color of the progressbar
        /// </summary>
        public Color ProgressColor
        {
            get => ProgressBar.ProgressColor;
            set => ProgressBar.ProgressColor = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
        }
    }
}
