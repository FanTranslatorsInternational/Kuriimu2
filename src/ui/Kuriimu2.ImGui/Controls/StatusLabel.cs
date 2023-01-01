using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Controls
{
    class StatusLabel : Component
    {
        private readonly Label _label;

        public bool IsSuccessful { get; private set; }

        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
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

        public void Report(bool isSuccessful, LocalizedString message)
        {
            if (message.IsEmpty)
                return;

            Text = message;
            IsSuccessful = isSuccessful;
        }

        public void Clear()
        {
            Text = string.Empty;
            IsSuccessful = true;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _label.TextColor = IsSuccessful ? ColorResources.TextSuccessful : ColorResources.TextFatal;

            _label.Update(contentRect);
        }
    }
}
