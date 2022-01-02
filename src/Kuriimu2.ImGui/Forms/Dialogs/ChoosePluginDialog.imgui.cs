using System;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class ChoosePluginDialog
    {
        private Label _msgLabel;
        private StackLayout _pluginList;
        private CheckBox _showAllPlugins;

        private Button _continueButton;
        private Button _viewRawButton;
        private Button _cancelButton;

        private void InitializeComponent()
        {
            #region Controls

            _msgLabel = new Label();
            _pluginList = new StackLayout { ItemSpacing = 4 };
            _showAllPlugins = new CheckBox { Caption = LocalizationResources.ChoosePluginShowAllResource() };

            _continueButton = new Button { Caption = LocalizationResources.ChoosePluginContinueResource(), Enabled = false };
            _viewRawButton = new Button { Caption = LocalizationResources.ChoosePluginRawBytesResource() };
            _cancelButton = new Button { Caption = LocalizationResources.ChoosePluginCancelResource() };

            #region Main layout

            var mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _msgLabel,
                    _pluginList,
                    _showAllPlugins,
                    new StackLayout
                    {
                        Size=new Size(1f,-1),
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

            var width = (int)Math.Ceiling(Application.Instance.MainForm.Width * .4f);
            var height = (int)Math.Ceiling(Application.Instance.MainForm.Height * .8f);
            Size = new Vector2(width, height);

            Caption = LocalizationResources.ChoosePluginTitleResource();
            Content = mainLayout;

            #endregion
        }
    }
}
