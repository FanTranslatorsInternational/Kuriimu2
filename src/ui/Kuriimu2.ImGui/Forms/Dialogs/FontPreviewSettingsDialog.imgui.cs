using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kaligraphy.Enums.Layout;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class FontPreviewSettingsDialog : Modal
    {
        private CheckBox _debugBoxCheck;
        private TextBox _spacingTextBox;
        private TextBox _lineHeightBox;
        private ComboBox<HorizontalTextAlignment> _alignmentComboBox;

        private void InitializeComponent()
        {
            #region Controls

            _debugBoxCheck = new CheckBox
            {
                Checked = Settings.ShowDebugBoxes
            };
            _spacingTextBox = new TextBox
            {
                Width = SizeValue.Absolute(100),
                AllowedCharacters = CharacterRestriction.Decimal,
                Text = $"{Settings.Spacing}"
            };
            _lineHeightBox = new TextBox
            {
                Width = SizeValue.Absolute(100),
                AllowedCharacters = CharacterRestriction.Decimal,
                Text = $"{Settings.LineHeight}"
            };
            _alignmentComboBox = new ComboBox<HorizontalTextAlignment>
            {
                MaxShowItems = 3
            };

            #endregion

            #region Layout

            var mainLayout = new TableLayout
            {
                Size = Size.Content,
                Spacing = new(4, 4),
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new Label(LocalizationResources.FontPreviewSettingsShowDebug),
                            _debugBoxCheck
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new Label(LocalizationResources.FontPreviewSettingsSpacing),
                            _spacingTextBox
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new Label(LocalizationResources.FontPreviewSettingsLineHeight),
                            _lineHeightBox
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new Label(LocalizationResources.FontPreviewSettingsAlignment),
                            _alignmentComboBox
                        }
                    }
                }
            };

            #endregion

            Content = mainLayout;
            Size = Size.Content;

            Caption = LocalizationResources.FontPreviewSettingsCaption;

            InitializeAlignment();
        }

        private void InitializeAlignment()
        {
            _alignmentComboBox.Items.Add(new DropDownItem<HorizontalTextAlignment>(HorizontalTextAlignment.Left, LocalizationResources.FontPreviewSettingsAlignmentLeft));
            _alignmentComboBox.Items.Add(new DropDownItem<HorizontalTextAlignment>(HorizontalTextAlignment.Center, LocalizationResources.FontPreviewSettingsAlignmentCenter));
            _alignmentComboBox.Items.Add(new DropDownItem<HorizontalTextAlignment>(HorizontalTextAlignment.Right, LocalizationResources.FontPreviewSettingsAlignmentRight));

            _alignmentComboBox.SelectedItem = _alignmentComboBox.Items.FirstOrDefault(Settings.HorizontalAlignment);
        }
    }
}
