using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Forms.Dialogs;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using Kuriimu2.ImGui.Resources;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kuriimu2.ImGui.Components
{
    internal partial class FontSetView
    {
        [GeneratedRegex(@"^\\u([a-fA-F0-9]{4})$", RegexOptions.Compiled)]
        private static partial Regex UnicodeRegex();

        private readonly FormInfo<IFontFilePluginState> _state;
        private readonly FontSet _set;

        public event EventHandler? Updated;
        public event EventHandler<CharacterInfo>? SelectedGlyphChanged;

        public FontSetView(FormInfo<IFontFilePluginState> state, FontSet set)
        {
            _state = state;
            _set = set;

            InitializeComponent(state.PluginState);

            _searchCharBox.TextChanged += SearchCharBox_TextChanged;
            _editBtn.Clicked += EditBtn_Clicked;
            _removeBtn.Clicked += RemoveBtn_Clicked;
            _remapBtn.Clicked += RemapBtn_Clicked;
            _changeBtn.Clicked += ChangeBtn_Clicked;

            Reset();
        }

        private void SearchCharBox_TextChanged(object? sender, EventArgs e)
        {
            if (_searchCharBox.Text is null)
                return;

            char? searchChar = GetCharacter(_searchCharBox.Text);
            if (!searchChar.HasValue)
                return;

            if (!_charLookup.TryGetValue(searchChar.Value, out GlyphElement? glyph))
                return;

            _glyphsLayout.ScrollToItem(glyph);
            SetSelectedGlyph(glyph);
        }

        private async void EditBtn_Clicked(object? sender, EventArgs e)
        {
            string selectedCharacters = string.Concat(_selectedCharacters.Select(c => c.CodePoint));

            var generationDialog = new FontGenerationDialog(_state.PluginState, _set, FontGenerationType.Edit, selectedCharacters);

            DialogResult result = await generationDialog.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
        }

        private async void RemoveBtn_Clicked(object? sender, EventArgs e)
        {
            if (_selectedCharacters.Count <= 0)
                return;

            DialogResult result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogFontRemoveCaption, LocalizationResources.DialogFontRemoveText);
            if (result is not DialogResult.Yes)
                return;

            if (_selectedCharacters.Count >= _set.Characters.Count)
            {
                ClearCharacters();
            }
            else
            {
                foreach (CharacterInfo character in _selectedCharacters)
                    RemoveCharacter(character);
            }

            _lastSelectedElement = null;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
        }

        private async void RemapBtn_Clicked(object? sender, EventArgs e)
        {
            var selectedCharacters = _selectedCharacters.OrderBy(c => c.CodePoint).ToArray();

            var remapDialog = new FontRemappingDialog(_set, selectedCharacters);
            var result = await remapDialog.ShowAsync();

            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
        }

        private async void ChangeBtn_Clicked(object? sender, EventArgs e)
        {
            if (_selectedElement is null)
                return;

            var result = await InputBox.ShowAsync(LocalizationResources.FontGenerateChangeCaption, string.Empty,
                $"{_selectedElement.CharacterInfo.CodePoint}", LocalizationResources.FontGenerateChangePlaceholder);
            if (result is null)
                return;

            var code = GetCharacter(result);

            if (!code.HasValue)
                return;

            if (_set.Characters.Any(x => x.CodePoint == code))
            {
                await MessageBox.ShowErrorAsync(LocalizationResources.FontGenerateChangeCaption,
                    LocalizationResources.FontGenerateChangeError(code.Value));
                return;
            }

            CharacterInfo? newCharacter = _state.PluginState.AttemptCreateCharacterInfo(code.Value);
            if (newCharacter is not null)
            {
                newCharacter.Glyph = _selectedElement.CharacterInfo.Glyph;
                newCharacter.GlyphPosition = _selectedElement.CharacterInfo.GlyphPosition;
                newCharacter.BoundingBox = _selectedElement.CharacterInfo.BoundingBox;
                newCharacter.ContentChanged = true;

                _state.PluginState.AttemptAddCharacter(_set, newCharacter);
            }

            RemoveCharacter(_selectedElement.CharacterInfo);

            _state.FormCommunicator.Update(true, false);

            UpdateState();
        }

        private void RemoveCharacter(CharacterInfo character)
        {
            if (!_state.PluginState.AttemptRemoveCharacter(_set, character))
                return;

            if (_infoLookup.TryGetValue(character, out GlyphElement? element))
                _glyphsLayout.Items.Remove(element);

            _charLookup.Remove(character.CodePoint);
            _infoLookup.Remove(character);

            _selectedCharacters.Remove(character);
        }

        private void ClearCharacters()
        {
            _state.PluginState.AttemptRemoveAll(_set);

            _charLookup.Clear();
            _infoLookup.Clear();

            _selectedCharacters.Clear();

            _glyphsLayout.Items.Clear();
        }

        private static char? GetCharacter(string searchText)
        {
            Match match = UnicodeRegex().Match(searchText);

            if (match.Groups.Count > 1)
                return (char)BinaryPrimitives.ReadInt16BigEndian(Convert.FromHexString(match.Groups[1].Value));

            if (searchText.Length is 1)
                return searchText[0];

            return null;
        }

        public void Reset()
        {
            SetGlyphs(_set.Characters);

            if (_set.Characters.Count > 0)
                SetSelectedGlyph(_set.Characters[0]);
        }

        private void UpdateState()
        {
            UpdateGlyphs(_set.Characters);

            if (_selectedElement is not null)
                OnSelectedGlyphChanged(_selectedElement.CharacterInfo);
            
            OnUpdated();
        }

        private void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectedGlyphChanged(CharacterInfo characterInfo)
        {
            SelectedGlyphChanged?.Invoke(this, characterInfo);
        }
    }
}
