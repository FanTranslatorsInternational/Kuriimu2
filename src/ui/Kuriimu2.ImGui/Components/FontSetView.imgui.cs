using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Support;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Kuriimu2.ImGui.Components
{
    internal partial class FontSetView : Component
    {
        private static readonly KeyCommand SelectMultipleGlyphsCommand = new(ImGuiKey.ModCtrl, ImGuiMouseButton.Left);
        private static readonly KeyCommand SelectGlyphRangeCommand = new(ImGuiKey.ModShift, ImGuiMouseButton.Left);

        private readonly Dictionary<CharacterInfo, GlyphElement> _infoLookup = [];
        private readonly Dictionary<char, GlyphElement> _charLookup = [];
        private readonly HashSet<CharacterInfo> _selectedCharacters = [];

        private GlyphElement? _selectedElement;
        private GlyphElement? _lastSelectedElement;

        private ImageButton _editBtn;
        private ImageButton _removeBtn;
        private ImageButton _remapBtn;
        private ImageButton _changeBtn;

        private TextBox _searchCharBox;
        private UniformZLayout _glyphsLayout;

        private StackLayout _glyphLayout;

        public override Size GetSize() => Size.Parent;

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _glyphLayout.Update(contentRect);
        }

        [MemberNotNull(nameof(_editBtn), nameof(_removeBtn), nameof(_remapBtn), nameof(_changeBtn))]
        [MemberNotNull(nameof(_glyphLayout), nameof(_searchCharBox), nameof(_glyphsLayout))]
        private void InitializeComponent(IFontFilePluginState fontState)
        {
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
                Tooltip = LocalizationResources.FontGenerateRemapCaption,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5)
            };
            _changeBtn = new ImageButton
            {
                Image = ImageResources.FontChange,
                Tooltip = LocalizationResources.FontGenerateChangeCaption,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = fontState.CanRemoveCharacter
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
                            _changeBtn,
                            new StackItem(_searchCharBox) { Size = Size.WidthAlign, HorizontalAlignment = HorizontalAlignment.Right },
                        }
                    },
                    _glyphsLayout
                }
            };
        }

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

            OnSelectedGlyphChanged(element.CharacterInfo);
        }
    }
}
