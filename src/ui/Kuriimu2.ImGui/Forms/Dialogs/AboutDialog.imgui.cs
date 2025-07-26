using System.Text.Json;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Localization;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class AboutDialog : Modal
    {
        private Label _titleLabel;
        private Label _versionLabel;
        private Label _descriptionLabel;

        private void InitializeComponent()
        {
            Size = new Size(SizeValue.Relative(.3f), SizeValue.Relative(.3f));

            _titleLabel = new Label { Text = LocalizationResources.ApplicationName };
            _versionLabel = new Label { Text = GetVersionText() };
            _descriptionLabel = new Label { Text = LocalizationResources.MenuAboutDescription };
            var mainLayout = new StackLayout
            {
                Size = Size,
                Alignment = Alignment.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                ItemSpacing = 10,
                Items =
                {
                    new StackItem(_titleLabel) {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
                    new StackItem(_versionLabel) {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
                    new StackItem(_descriptionLabel) {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
                }
            };

            Caption = LocalizationResources.MenuAboutTitle;
            Content = mainLayout;
        }

        private LocalizedString GetVersionText()
        {
            string manifest = BinaryResources.VersionManifest;
            var manifestObject = JsonSerializer.Deserialize<Manifest>(manifest);

            return LocalizationResources.MenuAboutVersion(manifestObject?.Version);
        }
    }
}
