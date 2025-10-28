using System;
using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Text.Editor;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using Veldrid;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class FontForm
    {
        private static readonly KeyCommand SelectMultipleGlyphsCommand = new(ModifierKeys.Control, MouseButton.Left);
        private static readonly KeyCommand SelectGlyphRangeCommand = new(ModifierKeys.Shift, MouseButton.Left);

        private readonly Dictionary<CharacterInfo, GlyphElement> _infoLookup = new();
        private readonly Dictionary<char, GlyphElement> _charLookup = new();

        private StackLayout _mainLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private Button _generateBtn;
        private ImageButton _editBtn;
        private ImageButton _removeBtn;
        private ImageButton _remapBtn;

        private TextEditor _previewTextEditor;
        private ZoomablePictureBox _textPreview;

        private ZoomableCharacterInfo _glyphBox;

        private ImageButton _exportBtn;
        private ImageButton _settingsBtn;

        private StackLayout _glyphLayout;
        private TextBox _searchCharBox;
        private UniformZLayout _glyphsLayout;
        private readonly HashSet<CharacterInfo> _selectedCharacters = [];
        private GlyphElement? _selectedElement;
        private GlyphElement? _lastSelectedElement;

        private void InitializeComponent(IFontFilePluginState fontState)
        {
            #region Controls

            _glyphBox = new ZoomableCharacterInfo
            {
                ShowBorder = true,
                BackgroundColor = ColorResources.GlyphBackground,
                Size = new Size(SizeValue.Parent, .75f)
            };

            _editBtn = new ImageButton
            {
                Image = ImageResources.FontEdit,
                Tooltip = LocalizationResources.FontGenerateEditCaption,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };
            _removeBtn = new ImageButton
            {
                Image = ImageResources.FontRemove,
                Tooltip = LocalizationResources.FontGenerateRemoveCaption,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = fontState.CanRemoveCharacter
            };
            _remapBtn = new ImageButton
            {
                Image = ImageResources.FontRemap,
                Tooltip = LocalizationResources.FontGenerateRemappingCaption,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };

            _searchCharBox = new TextBox
            {
                Width = SizeValue.Absolute(150),
                Placeholder = LocalizationResources.FontSearchPlaceholder,
            };

            _glyphsLayout = new UniformZLayout(new Vector2(36, 61))
            {
                ItemSpacing = new Vector2(4, 4),
                Size = Size.Parent
            };
            _glyphLayout = new StackLayout
            {
                ItemSpacing = 4,
                Size = Size.Parent,
                Alignment = Alignment.Vertical,
                Items =
                {
                    new StackLayout
                    {
                        ItemSpacing = 4,
                        Size = Size.WidthAlign,
                        Alignment = Alignment.Horizontal,
                        Items =
                        {
                            _editBtn,
                            _removeBtn,
                            _remapBtn,
                            new StackItem(_searchCharBox) { Size = Size.WidthAlign, HorizontalAlignment = HorizontalAlignment.Right },
                        }
                    },
                    _glyphsLayout
                }
            };

            _saveBtn = new ImageButton
            {
                Image = ImageResources.Save,
                Tooltip = LocalizationResources.MenuFileSave,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new(ModifierKeys.Control, Key.S, LocalizationResources.MenuFileSaveShortcut)
            };
            _saveAsBtn = new ImageButton
            {
                Image = ImageResources.SaveAs,
                Tooltip = LocalizationResources.MenuFileSaveAs,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new(Key.F12, LocalizationResources.MenuFileSaveAsShortcut)
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
                Enabled = fontState is { CanAddCharacter: true, CanRemoveCharacter: true }
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

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    fontDataLayout,
                    _glyphLayout
                }
            };
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
            _lastSelectedElement = null;
            _selectedElement = null;
            _selectedCharacters.Clear();

            _glyphsLayout.Items.Clear();
            _infoLookup.Clear();
            _charLookup.Clear();

            foreach (CharacterInfo character in characters)
            {
                var element = new GlyphElement(character)
                {
                    BackgroundColor = ColorResources.GlyphBackground
                };
                element.SelectedChanged += (_, _) => SetSelectedGlyph(element);

                _glyphsLayout.Items.Add(element);

                _infoLookup[character] = element;
                _charLookup[character.CodePoint] = element;
            }
        }

        private void UpdateGlyphs(IReadOnlyList<CharacterInfo> characters)
        {
            _glyphsLayout.Items.Clear();
            _infoLookup.Clear();
            _charLookup.Clear();

            foreach (CharacterInfo character in characters)
            {
                var element = new GlyphElement(character)
                {
                    BackgroundColor = ColorResources.GlyphBackground,
                    IsSelected = _selectedCharacters.Contains(character)
                };
                element.SelectedChanged += (_, _) => SetSelectedGlyph(element);

                _glyphsLayout.Items.Add(element);

                _infoLookup[character] = element;
                _charLookup[character.CodePoint] = element;
            }
        }

        private void SetSelectedGlyph(CharacterInfo charInfo)
        {
            if (!_infoLookup.TryGetValue(charInfo, out GlyphElement? element))
                return;

            SetSelectedGlyph(element);
        }

        private void SetSelectedGlyph(GlyphElement element)
        {
            if (SelectMultipleGlyphsCommand.IsPressed())
            {
                _selectedCharacters.Add(element.CharacterInfo);

                _lastSelectedElement = element;
            }
            else if (SelectGlyphRangeCommand.IsPressed())
            {
                if (_lastSelectedElement == null)
                    _lastSelectedElement = element;
                else
                {
                    var lastIndex = _glyphsLayout.Items.IndexOf(_lastSelectedElement);
                    var currentIndex = _glyphsLayout.Items.IndexOf(element);

                    foreach (CharacterInfo selectedCharacter in _selectedCharacters)
                    {
                        if (!_infoLookup.TryGetValue(selectedCharacter, out GlyphElement? selectedGlyph))
                            continue;

                        selectedGlyph.IsSelected = false;
                    }

                    _selectedCharacters.Clear();

                    for (var i = Math.Min(lastIndex, currentIndex); i <= Math.Max(lastIndex, currentIndex); i++)
                    {
                        var selectedGlyph = (GlyphElement)_glyphsLayout.Items[i];
                        _selectedCharacters.Add(selectedGlyph.CharacterInfo);

                        selectedGlyph.IsSelected = true;
                    }
                }
            }
            else
            {
                foreach (CharacterInfo selectedCharacter in _selectedCharacters)
                {
                    if (!_infoLookup.TryGetValue(selectedCharacter, out GlyphElement? selectedGlyph))
                        continue;

                    selectedGlyph.IsSelected = false;
                }

                _selectedCharacters.Clear();
                _selectedCharacters.Add(element.CharacterInfo);

                _lastSelectedElement = element;
            }

            _selectedElement = element;

            element.IsSelected = true;

            _glyphBox.SetCharacterInfo(element.CharacterInfo);
        }

        protected override void SetTabInactiveCore()
        {
            _glyphsLayout.SetTabInactive();
        }
    }
}
