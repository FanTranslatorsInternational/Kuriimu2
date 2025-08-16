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
using ImGui.Forms.Modals;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Files;
using Kuriimu2.ImGui.Resources;
using Point = SixLabors.ImageSharp.Point;

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
            object? data = _treeView.SelectedNode?.Data;
            if (data is null)
                return;

            if (data is not TranslatedTextEntry entry)
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

            IList<IList<CharacterData>> allParsedTranslatedTexts = entry.Page is not null
                ? GetParsedPageCharacters(entry.Page)
                : [deserializedText];

            _previewPages = GeneratePreviews(allParsedTranslatedTexts);
            _previewPageIndex = _previewPages?.Count >= 1 ? 0 : -1;

            entry.Entry.TextData = translatedData;
            entry.Entry.ContentChanged = true;

            _state.FormCommunicator.Update(true, false);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _fontFamilyBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private async void _previewBox_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_state.FileState.StateChanged)
            {
                DialogResult result = await MessageBox.ShowYesNoAsync(
                    LocalizationResources.DialogUnsavedChangesCaption, LocalizationResources.TextPreviewTextChanged);

                if (result is DialogResult.Yes)
                {
                    _serializedOriginalTexts.Clear();
                    _parsedTranslatedTexts.Clear();
                    _serializedTranslatedTexts.Clear();
                    _serializedControlTexts.Clear();

                    _previewPages = null;
                    _previewPageIndex = -1;

                    _selectedPreviewPlugin = _previewBox.SelectedItem?.Content;

                    foreach (TranslatedTextEntry translatedEntry in _translatedTextEntries.Where(e => e.Entry.ContentChanged))
                    {
                        translatedEntry.Entry.TextData = translatedEntry.OriginalTextData;
                        translatedEntry.Entry.ContentChanged = false;
                    }

                    _state.FormCommunicator.Update(true, false);

                    UpdateTextAndPreview();
                }
                else
                {
                    _previewBox.SelectedItemChanged -= _previewBox_SelectedItemChanged;
                    _previewBox.SelectedItem = _previewBox.Items.FirstOrDefault(i => i.Content == _selectedPreviewPlugin);
                    _previewBox.SelectedItemChanged += _previewBox_SelectedItemChanged;

                    return;
                }
            }
            else
            {
                UpdatePreview();
            }

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

            _editTextEditor.IsReadOnly = _treeView.SelectedNode?.Data is TranslatedTextEntryPage;

            _previousPageBtn.Enabled = _previewPageIndex > 0;
            _nextPageBtn.Enabled = _previewPageIndex < _previewPages?.Count - 1;
        }

        private void UpdateTextAndPreview()
        {
            object? entry = _treeView.SelectedNode?.Data;
            if (entry is null)
                return;

            if (entry is TranslatedTextEntryPage page)
            {
                PreprocessPage(page);

                _origTextEditor.SetText(string.Empty);
                _editTextEditor.SetText(string.Empty);
                _controlTextEditor.SetText(string.Empty);

                _previewPages = null;
                _previewPageIndex = -1;

                UpdatePreview();
            }
            else if (entry is TranslatedTextEntry translatedEntry)
            {
                PreprocessEntry(translatedEntry, out string serializedOriginalText, out string serializedTranslatedText,
                    out string serializedControlText, out IList<CharacterData> parsedTranslatedText);

                _origTextEditor.SetText(serializedOriginalText);
                _editTextEditor.SetText(serializedTranslatedText);
                _controlTextEditor.SetText(serializedControlText);

                IList<IList<CharacterData>> allParsedTranslatedTexts = translatedEntry.Page is not null
                    ? GetParsedPageCharacters(translatedEntry.Page)
                    : [parsedTranslatedText];

                _previewPages = GeneratePreviews(allParsedTranslatedTexts);
                _previewPageIndex = _previewPages?.Count >= 1 ? 0 : -1;

                UpdatePreview();
            }
        }

        private IList<IList<CharacterData>> GetParsedPageCharacters(TranslatedTextEntryPage page)
        {
            var result = new List<IList<CharacterData>>();

            foreach (TranslatedTextEntry entry in page.Entries)
            {
                if (!_parsedTranslatedTexts.TryGetValue(entry, out IList<CharacterData>? parsedCharacters))
                    continue;

                result.Add(parsedCharacters);
            }

            return result;
        }

        private void PreprocessPage(TranslatedTextEntryPage page)
        {
            foreach (TranslatedTextEntry entry in page.Entries)
                PersistEntry(entry, out _, out _, out _, out _);
        }

        private void PreprocessEntry(TranslatedTextEntry entry, out string serializedOriginalText, out string serializedTranslatedText,
            out string serializedControlText, out IList<CharacterData> parsedTranslatedText)
        {
            serializedOriginalText = string.Empty;
            serializedTranslatedText = string.Empty;
            serializedControlText = string.Empty;
            parsedTranslatedText = [];

            if (entry.Page is not null)
            {
                foreach (TranslatedTextEntry pageEntry in entry.Page.Entries)
                {
                    if (pageEntry == entry)
                        PersistEntry(entry, out serializedOriginalText, out serializedTranslatedText, out serializedControlText, out parsedTranslatedText);
                    else
                        PersistEntry(entry, out _, out _, out _, out _);
                }
            }
            else
            {
                PersistEntry(entry, out serializedOriginalText, out serializedTranslatedText, out serializedControlText, out parsedTranslatedText);
            }
        }

        private void PersistEntry(TranslatedTextEntry entry, out string serializedOriginalText, out string serializedTranslatedText,
            out string serializedControlText, out IList<CharacterData> parsedTranslatedText)
        {
            ICharacterParser parser = GetCharacterParser();
            ICharacterSerializer serializer = GetCharacterSerializer();

            if (!_serializedOriginalTexts.TryGetValue(entry, out serializedOriginalText!))
            {
                IList<CharacterData> parsedOriginalText = parser.Parse(entry.OriginalTextData, entry.Entry.Encoding);
                _serializedOriginalTexts[entry] = serializedOriginalText = serializer.Serialize(parsedOriginalText, true);
            }

            if (!_parsedTranslatedTexts.TryGetValue(entry, out parsedTranslatedText!))
                _parsedTranslatedTexts[entry] = parsedTranslatedText = parser.Parse(entry.Entry.TextData, entry.Entry.Encoding);

            if (!_serializedTranslatedTexts.TryGetValue(entry, out serializedTranslatedText!))
                _serializedTranslatedTexts[entry] = serializedTranslatedText = serializer.Serialize(parsedTranslatedText, true);

            if (!_serializedControlTexts.TryGetValue(entry, out serializedControlText!))
                _serializedControlTexts[entry] = serializedControlText = serializer.Serialize(parsedTranslatedText, false);
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

        private IList<Image<Rgba32>>? GeneratePreviews(IList<IList<CharacterData>> parsedTexts)
        {
            if (_previewBox.SelectedItem is not null)
                return _previewBox.SelectedItem.Content.CreatePreviewPages(parsedTexts).Result;

            FontFamily? fontFamily = _fontFamilyBox.SelectedItem?.Content;
            if (fontFamily is null)
                return null;

            var font = new Font(fontFamily, 15, FontStyle.Regular);
            var glyphProvider = new SystemFontGlyphProvider(font);

            var layouter = new TextLayouter(new LayoutOptions(), glyphProvider);

            IList<IList<TextLayoutLineData>> layoutLines = [];
            foreach (IList<CharacterData> parsedText in parsedTexts)
                layoutLines.Add(layouter.Create(parsedText));

            int imageWidth = layoutLines.Count <= 0 ? 0 : layoutLines.Max(t => t.Max(l => l.BoundingBox.Width));
            int imageHeight = layoutLines.Count <= 0 ? 0 : layoutLines.Sum(t => t.Sum(l => l.BoundingBox.Height));
            if (imageWidth <= 0 || imageHeight <= 0)
                return null;

            var image = new Image<Rgba32>(imageWidth + 1, imageHeight + 1);

            var initPoint = Point.Empty;
            foreach (IList<TextLayoutLineData> layoutLine in layoutLines)
            {
                TextLayoutData layout = layouter.Create(layoutLine, initPoint, image.Size);

                var renderer = new TextRenderer(new RenderOptions(), glyphProvider);
                renderer.Render(image, layout);

                initPoint = new Point(initPoint.X, initPoint.Y + layout.BoundingBox.Height);
            }

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
