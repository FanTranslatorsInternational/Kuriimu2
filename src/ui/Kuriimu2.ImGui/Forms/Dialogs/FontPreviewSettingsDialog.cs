using Kaligraphy.Enums.Layout;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class FontPreviewSettingsDialog
    {
        public FontPreviewSettings Settings { get; } = new();

        public FontPreviewSettingsDialog()
        {
            InitializeComponent();

            _debugBoxCheck.CheckChanged += DebugBoxCheck_CheckChanged;
            _spacingTextBox.TextChanged += SpacingTextBoxTextChanged;
            _lineHeightBox.TextChanged += LineHeightBox_TextChanged;
            _alignmentComboBox.SelectedItemChanged += AlignmentComboBox_SelectedItemChanged;
        }

        private void LineHeightBox_TextChanged(object? sender, System.EventArgs e)
        {
            if (!int.TryParse(_lineHeightBox.Text, out int lineHeight))
                return;

            Settings.LineHeight = lineHeight;
        }

        private void AlignmentComboBox_SelectedItemChanged(object? sender, System.EventArgs e)
        {
            Settings.HorizontalAlignment = _alignmentComboBox.SelectedItem?.Content ?? HorizontalTextAlignment.Left;
        }

        private void SpacingTextBoxTextChanged(object? sender, System.EventArgs e)
        {
            if (!int.TryParse(_spacingTextBox.Text, out int spacing))
                return;

            Settings.Spacing = spacing;
        }

        private void DebugBoxCheck_CheckChanged(object? sender, System.EventArgs e)
        {
            Settings.ShowDebugBoxes = _debugBoxCheck.Checked;
        }
    }
}
