using Kuriimu2.ImGui.Components;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class RawForm
    {
        private HexBox _hexBox;

        private void InitializeComponent()
        {
            _hexBox = new HexBox();
        }

        protected override void SetTabInactiveCore()
        {
            _hexBox.SetTabInactive();
        }
    }
}
