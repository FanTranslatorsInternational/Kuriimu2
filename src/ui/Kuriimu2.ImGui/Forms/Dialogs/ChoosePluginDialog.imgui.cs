using System;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class ChoosePluginDialog
    {
        private Label _msgLabel;
        private List<Component> _pluginList;
        private CheckBox _showAllPlugins;

        private Button _continueButton;
        private Button _viewRawButton;
        private Button _cancelButton;

        private void InitializeComponent()
        {
            #region Controls

            _msgLabel = new Label();
            _pluginList = new List<Component> { ItemSpacing = 4 };
            _showAllPlugins = new CheckBox { Text = LocalizationResources.DialogChoosePluginShowAll };

            _continueButton = new Button { Width = 70, Text = LocalizationResources.DialogChoosePluginContinue, Enabled = false };
            _viewRawButton = new Button { Padding = new Vector2(10, 2), Text = LocalizationResources.DialogChoosePluginViewRaw };
            _cancelButton = new Button { Width = 70, Text = LocalizationResources.DialogChoosePluginCancel };

            #region Main layout

            var mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _msgLabel,
                    new StackItem(_pluginList) { Size = global::ImGui.Forms.Models.Size.Parent },
                    _showAllPlugins,
                    new StackLayout
                    {
                        Size = global::ImGui.Forms.Models.Size.WidthAlign,
                        Alignment = Alignment.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ItemSpacing = 4,
                        Items =
                        {
                            _continueButton,
                            _viewRawButton,
                            _cancelButton
                        }
                    }
                }
            };

            #endregion

            #endregion

            #region Properties

            Size = new Size(SizeValue.Relative(.7f), SizeValue.Relative(.8f));

            Caption = LocalizationResources.DialogChoosePluginCaption;
            Content = mainLayout;

            #endregion
        }
    }
}
