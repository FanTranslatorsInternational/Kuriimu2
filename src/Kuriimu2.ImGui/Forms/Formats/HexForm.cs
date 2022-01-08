using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class HexForm : Component, IKuriimuForm
    {
        public HexForm(FormInfo<IHexState> formInfo)
        {
            InitializeComponent();

            _hexBox.Data = formInfo.PluginState.FileStream;
        }

        public override Size GetSize()
        {
            return _hexBox.GetSize();
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _hexBox.Update(contentRect);
        }

        #region IKuriimuForm implementation

        public void UpdateForm()
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
