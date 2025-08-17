using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class FontPreviewSettingsDialog
    {
        public FontPreviewSettings Settings { get; } = new();

        public FontPreviewSettingsDialog()
        {
            InitializeComponent();

            _debugBoxCheck.CheckChanged += _debugBoxCheck_CheckChanged;
            _spacingTextBox.TextChanged += SpacingTextBoxTextChanged;
            _lineHeightBox.TextChanged += _lineHeightBox_TextChanged;
            _alignmentComboBox.SelectedItemChanged += _alignmentComboBox_SelectedItemChanged;
        }

        private void _lineHeightBox_TextChanged(object? sender, System.EventArgs e)
        {
            if (!int.TryParse(_lineHeightBox.Text, out int lineHeight))
                return;

            Settings.LineHeight = lineHeight;
        }

        private void _alignmentComboBox_SelectedItemChanged(object? sender, System.EventArgs e)
        {
            Settings.HorizontalAlignment = _alignmentComboBox.SelectedItem.Content;
        }

        private void SpacingTextBoxTextChanged(object? sender, System.EventArgs e)
        {
            if (!int.TryParse(_spacingTextBox.Text, out int spacing))
                return;

            Settings.Spacing = spacing;
        }

        private void _debugBoxCheck_CheckChanged(object? sender, System.EventArgs e)
        {
            Settings.ShowDebugBoxes = _debugBoxCheck.Checked;
        }
    }
}
