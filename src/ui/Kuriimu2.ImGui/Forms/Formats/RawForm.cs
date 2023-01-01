using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class RawForm : Component, IKuriimuForm
    {
        public RawForm(FormInfo<IRawState> formInfo)
        {
            InitializeComponent();

            _hexBox.Data = formInfo.PluginState.FileStream;
        }

        #region Component implementation

        public override Size GetSize()
        {
            return _hexBox.GetSize();
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _hexBox.Update(contentRect);
        }

        #endregion

        #region IKuriimuForm implementation

        public void UpdateForm()
        {
        }

        public void ChangeTheme(Theme theme)
        {
        }

        public bool HasRunningOperations()
        {
            return false;
        }

        public void CancelOperations()
        {
        }

        #endregion
    }
}
