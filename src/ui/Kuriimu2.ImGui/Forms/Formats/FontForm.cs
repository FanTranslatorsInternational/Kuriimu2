using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Resources;
using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Rendering;
using Kaligraphy.Layout;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Plugin.File.Font;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Forms.Dialogs;
using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Models.Forms.Dialogs.Font;
using Kuriimu2.ImGui.Resources;
using Kuriimu2.ImGui.TextParsing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class FontForm : Component, IKuriimuForm
    {
        private readonly UnicodeCharacterParser _parser = new();
        private readonly FormInfo<IFontFilePluginState> _state;

        private FontPreviewSettingsDialog _previewSettingsDialog = new();
        private Image<Rgba32>? _generatedPreview;

        public FontForm(FormInfo<IFontFilePluginState> state)
        {
            _state = state;

            InitializeComponent(state.PluginState);

            _saveBtn.Clicked += _saveBtn_Clicked;
            _saveAsBtn.Clicked += _saveAsBtn_Clicked;

            _searchCharBox.TextChanged += _searchCharBox_TextChanged;
            _generateBtn.Clicked += _generateBtn_Clicked;
            _editBtn.Clicked += _editBtn_Clicked;
            _removeBtn.Clicked += _removeBtn_Clicked;
            _remapBtn.Clicked += _remapBtn_Clicked;

            _previewTextEditor.TextChanged += _previewTextEditor_TextChanged;

            _exportBtn.Clicked += _exportBtn_Clicked;
            _settingsBtn.Clicked += _settingsBtn_Clicked;

            _glyphBox.Zoom(20f);
            _previewTextEditor.SetText(LocalizationResources.FontPreviewPlaceholder);

            ResetState();
            UpdateFormInternal();
        }

        private async void _settingsBtn_Clicked(object? sender, EventArgs e)
        {
            await _previewSettingsDialog.ShowAsync();

            UpdateTextPreview();
        }

        private async void _exportBtn_Clicked(object? sender, EventArgs e)
        {
            if (_generatedPreview is null)
                return;

            // Select file to save at
            var sfd = new WindowsSaveFileDialog
            {
                Title = LocalizationResources.ImageMenuExportPng,
                InitialDirectory = GetLastDirectory(),
                InitialFileName = "preview.png"
            };

            if (await sfd.ShowAsync() is DialogResult.Ok)
                await _generatedPreview.SaveAsPngAsync(sfd.Files[0]);
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

            ResetState();
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
            var generationDialog = new FontGenerationDialog(_state.PluginState, FontGenerationType.Create, null);

            DialogResult result = await generationDialog.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            ResetState();
            UpdateFormInternal();
        }

        private async void _editBtn_Clicked(object? sender, EventArgs e)
        {
            string selectedCharacters = string.Concat(_selectedCharacters.Select(c => c.CodePoint));

            var generationDialog = new FontGenerationDialog(_state.PluginState, FontGenerationType.Edit, selectedCharacters);

            DialogResult result = await generationDialog.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
            UpdateFormInternal();
        }

        private async void _removeBtn_Clicked(object? sender, EventArgs e)
        {
            if (_selectedCharacters.Count <= 0)
                return;

            DialogResult result = await MessageBox.ShowYesNoAsync(LocalizationResources.DialogFontRemoveCaption, LocalizationResources.DialogFontRemoveText);
            if (result is not DialogResult.Yes)
                return;

            if (_selectedCharacters.Count >= _state.PluginState.Characters.Count)
            {
                _state.PluginState.AttemptRemoveAll();

                _charLookup.Clear();
                _infoLookup.Clear();

                _selectedCharacters.Clear();

                _glyphsLayout.Items.Clear();
            }
            else
            {
                foreach (CharacterInfo character in _selectedCharacters)
                {
                    if (!_state.PluginState.AttemptRemoveCharacter(character))
                        continue;

                    if (_infoLookup.TryGetValue(character, out GlyphElement? element))
                        _glyphsLayout.Items.Remove(element);

                    _charLookup.Remove(character.CodePoint);
                    _infoLookup.Remove(character);

                    _selectedCharacters.Remove(character);
                }
            }

            _lastSelectedElement = null;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
            UpdateFormInternal();
        }

        private async void _remapBtn_Clicked(object? sender, EventArgs e)
        {
            var selectedCharacters = _selectedCharacters.OrderBy(c => c.CodePoint).ToArray();

            var remapDialog = new FontRemappingDialog(_state.PluginState, selectedCharacters);
            var result = await remapDialog.ShowAsync();

            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            UpdateState();
            UpdateFormInternal();
        }

        private void _previewTextEditor_TextChanged(object? sender, string e)
        {
            UpdateTextPreview();
        }

        private void UpdateTextPreview()
        {
            _generatedPreview = GeneratePreview();

            _textPreview.Image = (_generatedPreview is null ? null : ImageResource.FromImage(_generatedPreview))!;
        }

        private Image<Rgba32>? GeneratePreview()
        {
            string text = _previewTextEditor.GetText();

            IList<CharacterData> parsedText = _parser.Parse(Encoding.UTF8.GetBytes(text), Encoding.UTF8);

            var glyphProvider = new FontPluginGlyphProvider(_state.PluginState.Characters);

            var layoutOptions = new LayoutOptions
            {
                TextSpacing = _previewSettingsDialog.Settings.Spacing,
                HorizontalAlignment = _previewSettingsDialog.Settings.HorizontalAlignment,
                LineHeight = _previewSettingsDialog.Settings.LineHeight
            };
            var layouter = new TextLayouter(layoutOptions, glyphProvider);
            IList<TextLayoutLineData> layoutLines = layouter.Create(parsedText);

            float imageWidth = layoutLines.Count <= 0 ? 0 : layoutLines.Max(l => l.BoundingBox.Width);
            float imageHeight = layoutLines.Count <= 0 ? 0 : layoutLines.Sum(l => l.BoundingBox.Height);
            if (imageWidth <= 0 || imageHeight <= 0)
                return null;

            var image = new Image<Rgba32>((int)imageWidth + 1, (int)imageHeight + 1);
            TextLayoutData layout = layouter.Create(layoutLines, Point.Empty, image.Size);

            var renderOptions = new RenderOptions
            {
                DrawBoundingBoxes = _previewSettingsDialog.Settings.ShowDebugBoxes
            };
            var renderer = new Kaligraphy.Rendering.TextRenderer(renderOptions, glyphProvider);
            renderer.Render(image, layout);

            return image;
        }

        #endregion

        #region Update methods

        private void ResetState()
        {
            SetGlyphs(_state.PluginState.Characters);

            if (_state.PluginState.Characters.Count > 0)
                SetSelectedGlyph(_state.PluginState.Characters[0]);

            UpdateTextPreview();
        }

        private void UpdateState()
        {
            UpdateGlyphs(_state.PluginState.Characters);

            if (_selectedElement is not null)
                _glyphBox.SetCharacterInfo(_selectedElement.CharacterInfo);

            UpdateTextPreview();
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

        private string GetLastDirectory()
        {
            var settingsDir = SettingsResources.LastDirectory;
            return string.IsNullOrEmpty(settingsDir) ? Path.GetFullPath(".") : settingsDir;
        }

        #endregion
    }
}
