using System;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using System.Collections.Generic;
using System.Linq;
using ImGui.Forms.Modals;
using Konnect.Contract.DataClasses.Plugin.File.Font;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class FontRemappingDialog
    {
        private readonly IReadOnlyList<CharacterInfo> _remapCharacters;

        private readonly List<GlyphElement> _selectedGlyphs = [];

        public FontRemappingDialog(IFontFilePluginState fontState, IReadOnlyList<CharacterInfo> remapCharacters)
        {
            InitializeComponent();

            _remapCharacters = remapCharacters;

            _remapButton!.Clicked += _remapButton_Clicked;

            SetGlyphs(fontState.Characters);
        }

        private void _remapButton_Clicked(object? sender, EventArgs e)
        {
            if (_selectedGlyphs.Count <= 0)
                return;

            for (var i = 0; i < _selectedGlyphs.Count; i++)
            {
                GlyphElement selectedGlyph = _selectedGlyphs[i];
                CharacterInfo remapCharacter = _remapCharacters[i];

                remapCharacter.Glyph = selectedGlyph.CharacterInfo.Glyph;
                remapCharacter.GlyphPosition = selectedGlyph.CharacterInfo.GlyphPosition;
                remapCharacter.BoundingBox = selectedGlyph.CharacterInfo.BoundingBox;
                remapCharacter.ContentChanged = true;
            }

            Close(DialogResult.Ok);
        }

        private void SetGlyphs(IReadOnlyList<CharacterInfo> characters)
        {
            _glyphsLayout.Items.Clear();

            foreach (CharacterInfo character in characters)
            {
                if (_remapCharacters.Contains(character))
                    continue;

                var element = new GlyphElement(character)
                {
                    BackgroundColor = ColorResources.GlyphBackground
                };
                element.SelectedChanged += (_, _) => SetGlyphRemapRange(element);

                _glyphsLayout.Items.Add(element);
            }
        }

        private void SetGlyphRemapRange(GlyphElement element)
        {
            foreach (GlyphElement selectedGlyph in _selectedGlyphs)
                selectedGlyph.IsSelected = false;

            _selectedGlyphs.Clear();

            int elementIndex = _glyphsLayout.Items.IndexOf(element);

            if (elementIndex < 0 || elementIndex >= _glyphsLayout.Items.Count)
            {
                _remapButton.Enabled = false;
                return;
            }

            for (int i = elementIndex; i < Math.Min(_glyphsLayout.Items.Count, elementIndex + _remapCharacters.Count); i++)
            {
                _selectedGlyphs.Add((GlyphElement)_glyphsLayout.Items[i]);
                ((GlyphElement)_glyphsLayout.Items[i]).IsSelected = true;
            }

            _remapButton.Enabled = true;
        }
    }
}
