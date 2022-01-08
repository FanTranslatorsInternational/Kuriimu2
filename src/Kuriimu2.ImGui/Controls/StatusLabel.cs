using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Controls
{
    class StatusLabel : Component
    {
        private readonly Label _label;

        public bool IsSuccessful { get; set; }

        public string Caption
        {
            get => _label.Caption;
            set => _label.Caption = value;
        }

        public SizeValue Width
        {
            get => _label.Width;
            set => _label.Width = value;
        }

        public StatusLabel()
        {
            _label = new Label();
        }

        public override Size GetSize()
        {
            return _label.GetSize();
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _label.TextColor = IsSuccessful ? ColorResources.TextSuccessful : ColorResources.TextFatal;

            _label.Update(contentRect);
        }
    }
}
