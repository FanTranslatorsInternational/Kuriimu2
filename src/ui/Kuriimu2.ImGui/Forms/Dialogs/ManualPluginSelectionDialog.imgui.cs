using System.Diagnostics.CodeAnalysis;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class ManualPluginSelectionDialog
    {
        private Label _msgLabel;
        private List<Component> _pluginList;
        private CheckBox _showAllPlugins;

        private Button _continueButton;
        private Button _cancelButton;

        [MemberNotNull(nameof(_msgLabel), nameof(_pluginList), nameof(_showAllPlugins))]
        [MemberNotNull(nameof(_continueButton), nameof(_cancelButton))]
        private void InitializeComponent()
        {
            #region Controls

            _msgLabel = new Label();
            _pluginList = new List<Component> { ItemSpacing = 4 };
            _showAllPlugins = new CheckBox { Text = LocalizationResources.DialogPluginsManualSelectionShowAll };

            _continueButton = new Button { Width = 70, Text = LocalizationResources.DialogPluginsManualSelectionContinue, Enabled = false };
            _cancelButton = new Button { Width = 70, Text = LocalizationResources.DialogPluginsManualSelectionCancel };

            #region Main layout

            var mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _msgLabel,
                    new StackItem(_pluginList) { Size = Size.Parent },
                    _showAllPlugins,
                    new StackLayout
                    {
                        Size = Size.WidthAlign,
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Items =
                        {
                            _continueButton,
                            _cancelButton
                        }
                    }
                }
            };

            #endregion

            #endregion

            #region Properties

            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            Caption = LocalizationResources.DialogPluginsManualSelectionCaption;
            Content = mainLayout;

            #endregion
        }
    }
}
