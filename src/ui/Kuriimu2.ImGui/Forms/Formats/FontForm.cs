using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    internal partial class FontForm : Component, IKuriimuForm
    {
        private readonly UnicodeCharacterParser _parser = new();
        private readonly FormInfo<IFontFilePluginState> _state;
        private readonly FontPreviewSettingsDialog _previewSettingsDialog = new();

        private Image<Rgba32>? _generatedPreview;

        public FontForm(FormInfo<IFontFilePluginState> state)
        {
            _state = state;

            InitializeComponent(state);

            _saveBtn.Clicked += SaveBtn_Clicked;
            _saveAsBtn.Clicked += SaveAsBtn_Clicked;

            _generateBtn.Clicked += GenerateBtn_Clicked;

            _previewTextEditor.TextChanged += PreviewTextEditor_TextChanged;

            _exportBtn.Clicked += ExportBtn_Clicked;
            _settingsBtn.Clicked += SettingsBtn_Clicked;

            _glyphBox.Zoom(20f);
            _previewTextEditor.SetText(LocalizationResources.FontPreviewPlaceholder);

            ResetState();
            UpdateFormInternal();
        }

        private async void SettingsBtn_Clicked(object? sender, EventArgs e)
        {
            await _previewSettingsDialog.ShowAsync();

            UpdateTextPreview();
        }

        private async void ExportBtn_Clicked(object? sender, EventArgs e)
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

        private async void SaveBtn_Clicked(object? sender, EventArgs e)
        {
            await Save(false);
        }

        private async void SaveAsBtn_Clicked(object? sender, EventArgs e)
        {
            await Save(true);
        }

        private async Task Save(bool saveAs)
        {
            await _state.FormCommunicator.Save(saveAs);

            ResetState();
            UpdateFormInternal();
        }

        private async void GenerateBtn_Clicked(object? sender, EventArgs e)
        {
            var set = GetSelectedSet();
            if (set is null)
                return;

            var generationDialog = new FontGenerationDialog(_state.PluginState, set, FontGenerationType.Create, null);

            DialogResult result = await generationDialog.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            _state.FormCommunicator.Update(true, false);

            ResetState();
            UpdateFormInternal();
        }

        private void PreviewTextEditor_TextChanged(object? sender, string e)
        {
            UpdateTextPreview();
        }

        private void UpdateTextPreview()
        {
            _generatedPreview = GeneratePreview();

            _textPreview.SetImage((_generatedPreview is null ? null : ImageResource.FromImage(_generatedPreview))!);
        }

        private Image<Rgba32>? GeneratePreview()
        {
            var set = GetSelectedSet();
            if (set is null)
                return null;

            string text = _previewTextEditor.GetText();

            IList<CharacterData> parsedText = _parser.Parse(Encoding.UTF8.GetBytes(text), Encoding.UTF8);

            var glyphProvider = new FontPluginGlyphProvider(set.Characters);

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
            var view = GetSelectedSetView();
            view?.Reset();

            UpdateTextPreview();
        }

        private void UpdateState()
        {
            UpdateTextPreview();
            UpdateFormInternal();
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

        private FontSetView? GetSelectedSetView()
        {
            if (_setViews is null)
                return null;

            return _setViews.Length <= 0 ? null : _setViews[_selectedSetIndex];
        }

        private FontSet? GetSelectedSet()
        {
            return _state.PluginState.Sets.Count <= 0 ? null : _state.PluginState.Sets[_selectedSetIndex];
        }

        private static string GetLastDirectory()
        {
            var settingsDir = SettingsResources.LastDirectory;
            return string.IsNullOrEmpty(settingsDir) ? Path.GetFullPath(".") : settingsDir;
        }

        #endregion
    }
}
