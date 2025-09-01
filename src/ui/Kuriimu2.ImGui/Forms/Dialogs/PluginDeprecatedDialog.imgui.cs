using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Plugin.File;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class PluginDeprecatedDialog : Modal
    {
        private void InitializeComponent(IDeprecatedFilePlugin deprecatedPlugin)
        {
            Size = new Size(SizeValue.Relative(.4f), SizeValue.Content);

            var titleLabel = new Label { Text = LocalizationResources.DialogDeprecatedTextGeneral(deprecatedPlugin.Metadata.Name) };
            var mainLayout = new StackLayout
            {
                Size = Size.WidthAlign,
                Alignment = Alignment.Vertical,
                ItemSpacing = 15,
                Items =
                {
                    titleLabel
                }
            };

            if (deprecatedPlugin.Alternatives.Length > 0)
            {
                var alternativesLabel = new Label { Text = LocalizationResources.DialogDeprecatedTextAlternatives };
                var alternativesLayout = new TableLayout
                {
                    Size = Size.WidthAlign,
                    Spacing = new Vector2(6)
                };

                foreach (DeprecatedPluginAlternative alternative in deprecatedPlugin.Alternatives)
                {
                    alternativesLayout.Rows.Add(new TableRow
                    {
                        Cells =
                        {
                            new Label(alternative.ToolName),
                            new TextBox { Text = alternative.Url, IsReadOnly = true }
                        }
                    });
                }

                var altLayout = new StackLayout
                {
                    Size = Size.WidthAlign,
                    Alignment = Alignment.Vertical,
                    ItemSpacing = 4,
                    Items =
                    {
                        alternativesLabel,
                        alternativesLayout
                    }
                };

                mainLayout.Items.Add(altLayout);
            }

            Caption = LocalizationResources.DialogDeprecatedCaption;
            Content = mainLayout;
        }
    }
}
