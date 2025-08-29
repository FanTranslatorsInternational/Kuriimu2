using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Localization;
using ImGui.Forms.Models;
using ImGuiNET;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Components
{
    class StatusLabel : Component
    {
        private readonly Label _label;

        public StatusKind StatusKind { get; private set; }

        public LocalizedString Text
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

        public void Report(StatusKind kind, LocalizedString message)
        {
            if (message.IsEmpty)
                return;

            Text = message;
            StatusKind = kind;
        }

        public void Clear()
        {
            Text = string.Empty;
            StatusKind = StatusKind.Info;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            switch (StatusKind)
            {
                case StatusKind.Info:
                    _label.TextColor = Style.GetColor(ImGuiCol.Text);
                    break;

                case StatusKind.Success:
                    _label.TextColor = ColorResources.TextSuccessful;
                    break;

                case StatusKind.Failure:
                    _label.TextColor = ColorResources.TextFatal;
                    break;
            }

            _label.Update(contentRect);
        }
    }
}
