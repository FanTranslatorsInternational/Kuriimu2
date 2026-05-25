using System;
using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGui.Forms.Localization;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    internal partial class FontForm
    {
        private StackLayout _mainLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private Button _generateBtn;

        private TextEditor _previewTextEditor;
        private ZoomablePictureBox _textPreview;

        private ZoomableCharacterInfo _glyphBox;

        private ImageButton _exportBtn;
        private ImageButton _settingsBtn;

        private FontSetView[]? _setViews;
        private int _selectedSetIndex;

        [MemberNotNull(nameof(_mainLayout))]
        [MemberNotNull(nameof(_saveBtn), nameof(_saveAsBtn), nameof(_generateBtn))]
        [MemberNotNull(nameof(_previewTextEditor), nameof(_textPreview), nameof(_glyphBox))]
        [MemberNotNull(nameof(_exportBtn), nameof(_settingsBtn))]
        private void InitializeComponent(FormInfo<IFontFilePluginState> state)
        {
            #region Controls

            _glyphBox = new ZoomableCharacterInfo
            {
                ShowBorder = true,
                BackgroundColor = ColorResources.GlyphBackground,
                Size = new Size(SizeValue.Parent, .75f)
            };

            _saveBtn = new ImageButton
            {
                Image = ImageResources.Save,
                Tooltip = LocalizationResources.MenuFileSave,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl, ImGuiKey.S, LocalizationResources.MenuFileSaveShortcut)
            };
            _saveAsBtn = new ImageButton
            {
                Image = ImageResources.SaveAs,
                Tooltip = LocalizationResources.MenuFileSaveAs,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new KeyCommand(ImGuiKey.F12, LocalizationResources.MenuFileSaveAsShortcut)
            };

            _exportBtn = new ImageButton
            {
                Image = ImageResources.ImageExport,
                Tooltip = LocalizationResources.FontPreviewExport,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };
            _settingsBtn = new ImageButton
            {
                Image = ImageResources.Settings,
                Tooltip = LocalizationResources.FontPreviewSettings,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };

            _generateBtn = new Button
            {
                Text = LocalizationResources.FontGenerateCaption,
                Width = SizeValue.Absolute(100),
                Enabled = state.PluginState is { CanAddCharacter: true, CanRemoveCharacter: true }
            };

            _previewTextEditor = new TextEditor();
            _textPreview = new ZoomablePictureBox
            {
                ShowBorder = true
            };

            #endregion

            var toolbarLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Size = Size.WidthAlign,
                Items =
                {
                    _saveBtn,
                    _saveAsBtn,
                    new StackItem(_generateBtn) { Size = Size.WidthAlign, HorizontalAlignment = HorizontalAlignment.Right }
                }
            };

            var textPreviewSettingsLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                Size = Size.WidthAlign,
                ItemSpacing = 4,
                Items =
                {
                    new StackItem(_exportBtn) { Size = Size.WidthAlign, HorizontalAlignment = HorizontalAlignment.Right },
                    _settingsBtn
                }
            };
            var textPreviewLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                Size = new Size(SizeValue.Parent, .25f),
                ItemSpacing = 4,
                Items =
                {
                    _previewTextEditor,
                    _textPreview
                }
            };

            var fontDataLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    toolbarLayout,
                    _glyphBox,
                    textPreviewSettingsLayout,
                    textPreviewLayout
                }
            };

            Component? glyphsView = null;
            switch (state.PluginState.Sets.Count)
            {
                case 1:
                    _setViews = new FontSetView[1];

                    _setViews[0] = new FontSetView(state, state.PluginState.Sets[0]);
                    _setViews[0].SelectedGlyphChanged += (_, e) => _glyphBox.SetCharacterInfo(e);
                    _setViews[0].Updated += (_, _) => UpdateState();

                    glyphsView = _setViews[0];
                    break;

                case > 1:
                    _setViews = new FontSetView[state.PluginState.Sets.Count];

                    var setViews = new TabControl { AllowClosingPages = false };
                    for (var i = 0; i < state.PluginState.Sets.Count; i++)
                    {
                        var set = state.PluginState.Sets[i];

                        var setView1 = new FontSetView(state, set);
                        setView1.SelectedGlyphChanged += (_, e) => _glyphBox.SetCharacterInfo(e);
                        setView1.Updated += (_, _) => UpdateState();

                        LocalizedString title = string.IsNullOrEmpty(set.Name) 
                            ? LocalizationResources.FontSetText(i + 1) 
                            : LocalizedString.FromText(set.Name);

                        setViews.AddPage(new TabPage(setView1) { Title = title });
                    }

                    setViews.SelectedPageChanged += (_, _) =>
                    {
                        _selectedSetIndex = Array.IndexOf([.. setViews.Pages], setViews.SelectedPage);
                        UpdateState();
                    };

                    glyphsView = setViews;
                    break;
            }

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    fontDataLayout,
                    glyphsView!
                }
            };
        }

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);
        }
    }
}
