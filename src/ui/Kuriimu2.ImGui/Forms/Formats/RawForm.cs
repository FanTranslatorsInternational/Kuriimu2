using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using ImGui.Forms.Support;
using Konnect.Contract.Plugin.File.Hex;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class RawForm : Component, IKuriimuForm
    {
        public RawForm(FormInfo<IHexFilePluginState> formInfo)
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
