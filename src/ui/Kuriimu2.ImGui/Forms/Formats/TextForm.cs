using Kuriimu2.ImGui.Interfaces;
using Kuriimu2.ImGui.Models;
using Konnect.Contract.Plugin.File.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImGui.Forms.Resources;
using Kaligraphy.Contract.DataClasses.Layout;
using Kaligraphy.Contract.DataClasses.Parsing;
using Kaligraphy.Contract.Parsing;
using Kaligraphy.DataClasses.Layout;
using Kaligraphy.DataClasses.Rendering;
using Kaligraphy.Layout;
using Kaligraphy.Parsing;
using Kaligraphy.Rendering;
using Kuriimu2.ImGui.Models.Forms.Formats;
using System;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Files;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class TextForm : IKuriimuForm
    {
        private readonly FormInfo<ITextFilePluginState> _state;
        private readonly IPluginManager _pluginManager;
        private readonly IFileManager _fileManager;

        private readonly Dictionary<TranslatedTextEntry, string> _serializedOriginalTexts = [];
        private readonly Dictionary<TranslatedTextEntry, IList<CharacterData>> _parsedTranslatedTexts = [];
        private readonly Dictionary<TranslatedTextEntry, string> _serializedTranslatedTexts = [];
        private readonly Dictionary<TranslatedTextEntry, string> _serializedControlTexts = [];

        private IList<CharacterData>? _selectedParsedTranslatedText;

        private IList<Image<Rgba32>>? _previewPages;
        private int _previewPageIndex = -1;

        public TextForm(FormInfo<ITextFilePluginState> state, IPluginManager pluginManager, IFileManager fileManager)
        {
            _state = state;
            _pluginManager = pluginManager;
            _fileManager = fileManager;

            InitializeComponent();

            _saveBtn.Clicked += _saveBtn_Clicked;
            _saveAsBtn.Clicked += _saveAsBtn_Clicked;
            _editTextEditor.TextChanged += _editTextEditor_TextChanged;
            _fontFamilyBox.SelectedItemChanged += _fontFamilyBox_SelectedItemChanged;
            _previewBox.SelectedItemChanged += _previewBox_SelectedItemChanged;
            _treeView.SelectedNodeChanged += _treeView_SelectedNodeChanged;
            _previousPageBtn.Clicked += _previousPageBtn_Clicked;
            _nextPageBtn.Clicked += _nextPageBtn_Clicked;

            UpdateTextAndPreview();
            UpdateFormInternal();
        }

        private void _previousPageBtn_Clicked(object? sender, EventArgs e)
        {
            _previewPageIndex = Math.Max(0, _previewPageIndex - 1);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _nextPageBtn_Clicked(object? sender, EventArgs e)
        {
            _previewPageIndex = Math.Min((_previewPages?.Count ?? 0) - 1, _previewPageIndex + 1);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _treeView_SelectedNodeChanged(object? sender, EventArgs e)
        {
            UpdateTextAndPreview();
            UpdateFormInternal();

            _textPreview.Reset();
        }

        private void _editTextEditor_TextChanged(object? sender, string e)
        {
            TranslatedTextEntry? entry = _treeView.SelectedNode?.Data;
            if (entry is null)
                return;

            string translatedText = _editTextEditor.GetText();

            ICharacterComposer composer = GetCharacterComposer();
            ICharacterDeserializer deserializer = GetCharacterDeserializer();
            ICharacterSerializer serializer = GetCharacterSerializer();

            IList<CharacterData> deserializedText = deserializer.Deserialize(translatedText);
            byte[] translatedData = composer.Compose(deserializedText, entry.Entry.Encoding);
            string serializedControlText = serializer.Serialize(deserializedText, false);

            _serializedTranslatedTexts[entry] = translatedText;
            _parsedTranslatedTexts[entry] = deserializedText;
            _serializedControlTexts[entry] = serializedControlText;

            _selectedParsedTranslatedText = deserializedText;

            _previewPages = GeneratePreviews(_selectedParsedTranslatedText);
            _previewPageIndex = _previewPages?.Count >= 1 ? 0 : -1;

            entry.Entry.TextData = translatedData;
            entry.Entry.ContentChanged = true;

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _fontFamilyBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void _previewBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

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
            bool isSaved = await _state.FormCommunicator.Save(saveAs);
            if (!isSaved)
                return;

            foreach (TextEntry textEntry in _state.PluginState.Texts)
                textEntry.ContentChanged = false;

            UpdateFormInternal();
        }

        private void UpdateFormInternal()
        {
            // Update save button enablement
            _saveBtn.Enabled = _state is { CanSave: true, FileState.StateChanged: true };
            _saveAsBtn.Enabled = _state is { CanSave: true, FileState: { StateChanged: true, ParentFileState: null } };

            _previousPageBtn.Enabled = _previewPageIndex > 0;
            _nextPageBtn.Enabled = _previewPageIndex < _previewPages?.Count - 1;
        }

        private void UpdateTextAndPreview()
        {
            TranslatedTextEntry? entry = _treeView.SelectedNode?.Data;
            if (entry is null)
                return;

            ICharacterParser parser = GetCharacterParser();
            ICharacterSerializer serializer = GetCharacterSerializer();

            if (!_serializedOriginalTexts.TryGetValue(entry, out string? serializedOriginalText))
            {
                IList<CharacterData> parsedOriginalText = parser.Parse(entry.OriginalTextData, entry.Entry.Encoding);
                _serializedOriginalTexts[entry] = serializedOriginalText = serializer.Serialize(parsedOriginalText, true);
            }

            if (!_parsedTranslatedTexts.TryGetValue(entry, out IList<CharacterData>? parsedTranslatedText))
                _parsedTranslatedTexts[entry] = parsedTranslatedText = parser.Parse(entry.Entry.TextData, entry.Entry.Encoding);

            if (!_serializedTranslatedTexts.TryGetValue(entry, out string? serializedTranslatedText))
                _serializedTranslatedTexts[entry] = serializedTranslatedText = serializer.Serialize(parsedTranslatedText, true);

            if (!_serializedControlTexts.TryGetValue(entry, out string? serializedControlText))
                _serializedControlTexts[entry] = serializedControlText = serializer.Serialize(parsedTranslatedText, false);

            _origTextEditor.SetText(serializedOriginalText);
            _editTextEditor.SetText(serializedTranslatedText);
            _controlTextEditor.SetText(serializedControlText);

            _selectedParsedTranslatedText = parsedTranslatedText;

            _previewPages = GeneratePreviews(_selectedParsedTranslatedText);
            _previewPageIndex = _previewPages?.Count >= 1 ? 0 : -1;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            Image<Rgba32>? preview = GetPreviewPage();
            if (preview is null)
            {
                _textPreview.Image = null;
                return;
            }

            _textPreview.Image = ImageResource.FromImage(preview);
        }

        private Image<Rgba32>? GetPreviewPage()
        {
            if (_previewPages is null || _previewPageIndex < 0 || _previewPageIndex >= _previewPages.Count)
                return null;

            return _previewPages[_previewPageIndex];
        }

        private IList<Image<Rgba32>>? GeneratePreviews(IList<CharacterData> parsedText)
        {
            if (_previewBox.SelectedItem is not null)
                return _previewBox.SelectedItem.Content.CreatePreviewPages(parsedText).Result;

            FontFamily? fontFamily = _fontFamilyBox.SelectedItem?.Content;
            if (fontFamily is null)
                return null;

            var font = new Font(fontFamily, 15, FontStyle.Regular);
            var glyphProvider = new SystemFontGlyphProvider(font);

            var layouter = new TextLayouter(new LayoutOptions(), glyphProvider);
            IList<TextLayoutLineData> layoutLines = layouter.Create(parsedText);

            int imageWidth = layoutLines.Count <= 0 ? 0 : layoutLines.Max(l => l.BoundingBox.Width);
            int imageHeight = layoutLines.Count <= 0 ? 0 : layoutLines.Sum(l => l.BoundingBox.Height);
            if (imageWidth <= 0 || imageHeight <= 0)
                return null;

            var image = new Image<Rgba32>(imageWidth + 1, imageHeight + 1);
            TextLayoutData layout = layouter.Create(layoutLines, image.Size);

            var renderer = new TextRenderer(new RenderOptions(), glyphProvider);
            renderer.Render(image, layout);

            return [image];
        }

        private ICharacterParser GetCharacterParser()
        {
            return _previewBox.SelectedItem?.Content.Parser ?? new CharacterParser();
        }

        private ICharacterSerializer GetCharacterSerializer()
        {
            return _previewBox.SelectedItem?.Content.Serializer ?? new CharacterSerializer();
        }

        private ICharacterComposer GetCharacterComposer()
        {
            return _previewBox.SelectedItem?.Content.Composer ?? new CharacterComposer();
        }

        private ICharacterDeserializer GetCharacterDeserializer()
        {
            return _previewBox.SelectedItem?.Content.Deserializer ?? new CharacterDeserializer();
        }

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
    }
}
