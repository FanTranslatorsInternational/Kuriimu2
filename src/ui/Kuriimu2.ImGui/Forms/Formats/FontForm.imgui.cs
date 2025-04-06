using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Forms.Dialogs.Font;
using Kuriimu2.ImGui.Resources;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class FontForm
    {
        private readonly Dictionary<CharacterInfo, GlyphElement> _charLookup = new();

        private StackLayout _mainLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private Button _generateBtn;

        private Label _baseLineLbl;
        private Label _baseLineTextLbl;
        private Label _descentLineLbl;
        private Label _descentLineTextLbl;

        private ZoomableCharacterInfo _glyphBox;

        private ZLayout _glyphsLayout;
        private GlyphElement? _selectedElement;

        private FontGenerationDialog _generationDialog;

        private void InitializeComponent(IFontFilePluginState fontState)
        {
            #region Controls

            _generationDialog = new FontGenerationDialog(fontState);

            _glyphBox = new ZoomableCharacterInfo
            {
                ShowBorder = true,
                BackgroundColor = ColorResources.GlyphBackground
            };

            _glyphsLayout = new ZLayout
            {
                ItemSpacing = new Vector2(4, 4),
                Size = Size.Parent
            };

            _saveBtn = new ImageButton { Image = Resources.ImageResources.Save, Tooltip = LocalizationResources.MenuFileSave, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5), Enabled = false };
            _saveAsBtn = new ImageButton { Image = Resources.ImageResources.SaveAs, Tooltip = LocalizationResources.MenuFileSaveAs, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5), Enabled = false };

            _generateBtn = new Button { Text = LocalizationResources.FontGenerateCaption, Width = SizeValue.Absolute(100), Enabled = fontState is { CanAddCharacter: true, CanRemoveCharacter: true } };

            _baseLineLbl = new Label { Text = LocalizationResources.FontLabelBaseLine };
            _descentLineLbl = new Label { Text = LocalizationResources.FontLabelDescentLine };

            _baseLineTextLbl = new Label();
            _descentLineTextLbl = new Label();

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

            var fontInfoLayout = new TableLayout
            {
                Spacing = new Vector2(4, 4),
                Size = Size.WidthAlign,
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            _baseLineLbl,
                            _descentLineLbl
                        }
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            _baseLineTextLbl,
                            _descentLineTextLbl
                        }
                    }
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
                    fontInfoLayout
                }
            };

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    fontDataLayout,
                    _glyphsLayout
                }
            };

            _glyphBox.Zoom(20f);
        }

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);
        }

        #endregion

        private void SetGlyphs(IReadOnlyList<CharacterInfo> characters)
        {
            _glyphsLayout.Items.Clear();
            _charLookup.Clear();

            foreach (CharacterInfo character in characters)
            {
                var element = new GlyphElement(character)
                {
                    BackgroundColor = ColorResources.GlyphBackground
                };
                element.SelectedChanged += (_, _) => SetSelectedGlyph(element);

                _glyphsLayout.Items.Add(element);

                _charLookup[character] = element;
            }
        }

        private void SetSelectedGlyph(CharacterInfo charInfo)
        {
            if (!_charLookup.TryGetValue(charInfo, out GlyphElement? element))
                return;

            SetSelectedGlyph(element);
        }

        private void SetSelectedGlyph(GlyphElement element)
        {
            if (_selectedElement != null)
                _selectedElement.IsSelected = false;

            _selectedElement = element;
            element.IsSelected = true;

            _glyphBox.SetCharacterInfo(element.CharacterInfo);
        }

        private void SetFontInformation(IFontFilePluginState state)
        {
            _baseLineTextLbl.Text = $"{state.Baseline}";
            _descentLineTextLbl.Text = $"{state.DescentLine}";
        }
    }
}
