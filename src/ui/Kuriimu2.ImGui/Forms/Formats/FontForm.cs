using System;
using System.Buffers.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Modals;
using Konnect.Contract.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class FontForm : Component, IKuriimuForm
    {
        private readonly FormInfo<IFontFilePluginState> _state;

        public FontForm(FormInfo<IFontFilePluginState> state)
        {
            _state = state;

            InitializeComponent(state.PluginState);

            _saveBtn.Clicked += _saveBtn_Clicked;
            _saveAsBtn.Clicked += _saveAsBtn_Clicked;

            _searchCharBox.TextChanged += _searchCharBox_TextChanged;
            _generateBtn.Clicked += _generateBtn_Clicked;

            UpdateState();
            UpdateFormInternal();
        }

        #region Events

        private async void _saveBtn_Clicked(object sender, EventArgs e)
        {
            await Save(false);
        }

        private async void _saveAsBtn_Clicked(object sender, EventArgs e)
        {
            await Save(true);
        }

        private async Task Save(bool saveAs)
        {
            await _state.FormCommunicator.Save(saveAs);

            UpdateState();
            UpdateFormInternal();
        }

        private void _searchCharBox_TextChanged(object? sender, EventArgs e)
        {
            char? searchChar = GetCharacter(_searchCharBox.Text);
            if (!searchChar.HasValue)
                return;

            if (!_charLookup.TryGetValue(searchChar.Value, out GlyphElement? glyph))
                return;

            _glyphsLayout.ScrollToItem(glyph);
            SetSelectedGlyph(glyph);
        }

        private async void _generateBtn_Clicked(object sender, EventArgs e)
        {
            DialogResult result = await _generationDialog.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            UpdateState();
            UpdateFormInternal();
        }

        #endregion

        #region Update methods

        private void UpdateState()
        {
            SetFontInformation(_state.PluginState);

            SetGlyphs(_state.PluginState.Characters);

            if (_state.PluginState.Characters.Count > 0)
                SetSelectedGlyph(_state.PluginState.Characters[0]);
        }

        private void UpdateFormInternal()
        {
            // Update save button enablement
            var canSave = _state.FileState.PluginState.CanSave;

            _saveBtn.Enabled = canSave && _state.FileState.StateChanged;
            _saveAsBtn.Enabled = canSave && _state.FileState is { StateChanged: true, ParentFileState: null };
        }

        #endregion

        #region IKuriimuForm implementation

        public void UpdateForm()
        {
            UpdateFormInternal();
        }

        public bool HasRunningOperations()
        {
            return false;
        }

        public void CancelOperations()
        {
        }

        #endregion

        #region Support

        private char? GetCharacter(string searchText)
        {
            var regex = new Regex(@"^\\u([a-fA-F0-9]{4})$");
            Match match = regex.Match(searchText);

            if (match.Groups.Count > 1)
                return (char)BinaryPrimitives.ReadInt16BigEndian(Convert.FromHexString(match.Groups[1].Value));

            if (searchText.Length is 1)
                return searchText[0];

            return null;
        }

        #endregion
    }
}
