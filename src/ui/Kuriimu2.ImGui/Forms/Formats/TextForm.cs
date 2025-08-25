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
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.Game;
using Kuriimu2.ImGui.Resources;
using Point = SixLabors.ImageSharp.Point;
using ImGui.Forms.Models.IO;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class TextForm : IKuriimuForm
    {
        private static readonly KeyCommand DeleteCommand = new(Key.Delete);

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

            _renameEntryButton.Clicked += _renameEntryButton_Clicked;
            _addEntryButton.Clicked += _addEntryButton_Clicked;
            _deleteEntryButton.Clicked += _deleteEntryButton_Clicked;

            Task.Run(StartupForm);
        }

        private async Task StartupForm()
        {
            await UpdateTextAndPreview();
            UpdateFormInternal();
        }

        private async void _renameEntryButton_Clicked(object? sender, EventArgs e)
        {
            if (!_state.PluginState.CanRenameEntry)
                return;

            await RenameSelectedEntry();
        }

        private async Task RenameSelectedEntry()
        {
            TreeNode<object> node = _treeView.SelectedNode;

            if (node?.Data is not TranslatedTextEntry entry)
                return;

            await RenameEntry(entry, node);
        }

        private async Task RenameEntry(TranslatedTextEntry entry, TreeNode<object> node)
        {
            string newName = await InputBox.ShowAsync(LocalizationResources.TextRenameCaption, LocalizationResources.TextRenameText,
                entry.Entry.Name ?? string.Empty);

            if (string.IsNullOrEmpty(newName))
            {
                _state.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.TextStatusRenameFailure);
                return;
            }

            bool wasRenamed = _state.PluginState.AttemptRenameEntry(entry.Entry, newName);
            if (!wasRenamed)
            {
                _state.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.TextStatusRenameFailure);
                return;
            }

            entry.Entry.ContentChanged = true;

            node.Text = newName;
            node.TextColor = ColorResources.Changed;

            _state.FormCommunicator.Update(true, false);
            _state.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.TextStatusRenameSuccess);

            UpdateFormInternal();
        }

        private async void _addEntryButton_Clicked(object? sender, EventArgs e)
        {
            if (!_state.PluginState.CanAddEntry)
                return;

            await AddSelectedEntry();
        }

        private async Task AddSelectedEntry()
        {
            TreeNode<object> node = _treeView.SelectedNode;

            switch (node?.Data)
            {
                case TranslatedTextEntry entry:
                    await AddEntry(entry, node);
                    break;

                case TranslatedTextEntryPage page:
                    await AddEntry(page, node);
                    break;
            }
        }

        private async Task AddEntry(TranslatedTextEntry entry, TreeNode<object> node)
        {
            IList<TreeNode<object>> nodes = entry.Page is null ? _treeView.Nodes : node.Parent.Nodes;

            TextEntry? newEntry = _state.PluginState.AttemptCreateEntry(entry.Page?.Page);
            if (newEntry is null)
                return;

            if (_state.PluginState.AttemptCanSetNewEntryName)
                newEntry.Name = await InputBox.ShowAsync(LocalizationResources.TextRenameCaption, LocalizationResources.TextRenameText, string.Empty);

            bool wasAdded = _state.PluginState.AttemptAddEntry(newEntry, entry.Page?.Page);

            if (!wasAdded)
            {
                _state.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.TextStatusAddFailure);
                return;
            }

            newEntry.ContentChanged = true;

            var translatedEntry = new TranslatedTextEntry
            {
                Entry = newEntry,
                OriginalTextData = newEntry.TextData,
                Page = entry.Page
            };
            _translatedTextEntries.Add(translatedEntry);

            entry.Page?.Entries.Add(translatedEntry);

            nodes.Add(new TreeNode<object>
            {
                Text = newEntry.Name ?? $"no_name_{nodes.Count:00}",
                Data = translatedEntry,
                TextColor = ColorResources.Changed,
                IsExpanded = true
            });

            _state.FormCommunicator.Update(true, false);
            _state.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.TextStatusAddSuccess);

            _previewPages = null;
            _previewPageIndex = -1;

            await UpdateTextAndPreview();
            UpdateFormInternal();
        }

        private async Task AddEntry(TranslatedTextEntryPage entryPage, TreeNode<object> node)
        {
            TextEntry? newEntry = _state.PluginState.AttemptCreateEntry(entryPage.Page);
            if (newEntry is null)
                return;

            if (_state.PluginState.AttemptCanSetNewEntryName)
                newEntry.Name = await InputBox.ShowAsync(LocalizationResources.TextRenameCaption, LocalizationResources.TextRenameText, string.Empty);

            bool wasAdded = _state.PluginState.AttemptAddEntry(newEntry, entryPage.Page);

            if (!wasAdded)
            {
                _state.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.TextStatusAddFailure);
                return;
            }

            newEntry.ContentChanged = true;

            var translatedEntry = new TranslatedTextEntry
            {
                Entry = newEntry,
                OriginalTextData = newEntry.TextData,
                Page = entryPage
            };
            _translatedTextEntries.Add(translatedEntry);

            entryPage.Entries.Add(translatedEntry);

            node.Nodes.Add(new TreeNode<object>
            {
                Text = newEntry.Name ?? $"no_name_{node.Nodes.Count:00}",
                Data = translatedEntry,
                TextColor = ColorResources.Changed,
                IsExpanded = true
            });

            _state.FormCommunicator.Update(true, false);
            _state.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.TextStatusAddSuccess);

            _previewPages = null;
            _previewPageIndex = -1;

            await UpdateTextAndPreview();
            UpdateFormInternal();
        }

        private void _deleteEntryButton_Clicked(object? sender, EventArgs e)
        {
            if (!_state.PluginState.CanRemoveEntry)
                return;

            DeleteSelectedEntry();
        }

        private void DeleteSelectedEntry()
        {
            TreeNode<object> node = _treeView.SelectedNode;

            switch (node?.Data)
            {
                case TranslatedTextEntry entry:
                    DeleteEntry(entry, node);
                    break;

                case TranslatedTextEntryPage entryPage:
                    DeleteEntry(entryPage, node);
                    break;
            }
        }

        private void DeleteEntry(TranslatedTextEntry entry, TreeNode<object> node)
        {
            bool wasRemoved = _state.PluginState.AttemptRemoveEntry(entry.Entry, entry.Page?.Page);
            if (!wasRemoved)
            {
                _state.FormCommunicator.ReportStatus(StatusKind.Failure, LocalizationResources.TextStatusDeleteFailure);
                return;
            }

            TreeNode<object>? selectedNode = null;
            if (entry.Page is null)
            {
                int nodeIndex = _treeView.Nodes.IndexOf(node);

                _treeView.Nodes.Remove(node);

                if (_treeView.Nodes.Count > 0)
                    selectedNode = _treeView.Nodes[Math.Max(0, nodeIndex - 1)];
            }
            else
            {
                TreeNode<object> pageNode = node.Parent;

                int nodeIndex = pageNode.Nodes.IndexOf(node);
                int pageIndex = _treeView.Nodes.IndexOf(pageNode);

                pageNode.Nodes.Remove(node);

                entry.Page.Entries.Remove(entry);
                if (entry.Page.Entries.Count <= 0)
                    _treeView.Nodes.Remove(pageNode);

                if (pageNode.Nodes.Count > 0)
                    selectedNode = pageNode.Nodes[Math.Max(0, nodeIndex - 1)];
                else if (_treeView.Nodes.Count > 0)
                    selectedNode = _treeView.Nodes[Math.Max(0, pageIndex - 1)];
            }

            _state.FormCommunicator.Update(true, false);
            _state.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.TextStatusDeleteSuccess);

            _previewPages = null;
            _previewPageIndex = -1;

            _serializedOriginalTexts.Remove(entry);
            _serializedTranslatedTexts.Remove(entry);
            _serializedControlTexts.Remove(entry);
            _parsedTranslatedTexts.Remove(entry);

            _treeView.SelectedNode = selectedNode;
        }

        private void DeleteEntry(TranslatedTextEntryPage entryPage, TreeNode<object> node)
        {
            TreeNode<object>[] nodes = node.Nodes.ToArray();
            foreach (TreeNode<object> entryNode in nodes)
            {
                var entry = (TranslatedTextEntry)entryNode.Data;

                bool wasRemoved = _state.PluginState.AttemptRemoveEntry(entry.Entry, entryPage.Page);
                if (!wasRemoved)
                    continue;

                entryPage.Entries.Remove(entry);
                node.Nodes.Remove(entryNode);

                _serializedOriginalTexts.Remove(entry);
                _serializedTranslatedTexts.Remove(entry);
                _serializedControlTexts.Remove(entry);
                _parsedTranslatedTexts.Remove(entry);
            }

            int nodeIndex = _treeView.Nodes.IndexOf(node);

            _treeView.Nodes.Remove(node);

            TreeNode<object>? selectedNode = null;
            if (_treeView.Nodes.Count > 0)
                selectedNode = _treeView.Nodes[Math.Max(0, nodeIndex - 1)];

            _state.FormCommunicator.Update(true, false);
            _state.FormCommunicator.ReportStatus(StatusKind.Success, LocalizationResources.TextStatusDeleteSuccess);

            _previewPages = null;
            _previewPageIndex = -1;

            _treeView.SelectedNode = selectedNode;
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

        private async void _treeView_SelectedNodeChanged(object? sender, EventArgs e)
        {
            await UpdateTextAndPreview();
            UpdateFormInternal();

            _textPreview.Reset();
        }

        private async void _editTextEditor_TextChanged(object? sender, string e)
        {
            object? data = _treeView.SelectedNode?.Data;
            if (data is null)
                return;

            if (data is not TranslatedTextEntry entry)
                return;

            SetGamePreviewState(entry);

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

            _controlTextEditor.SetText(serializedControlText);

            IList<IList<CharacterData>> allParsedTranslatedTexts = entry.Page is not null
                ? GetParsedPageCharacters(entry.Page)
                : [deserializedText];

            _previewPages = await GeneratePreviews(allParsedTranslatedTexts);
            _previewPageIndex = _previewPages?.Count >= 1 ? 0 : -1;

            entry.Entry.TextData = translatedData;
            entry.Entry.ContentChanged = true;

            if (_treeView.SelectedNode is not null)
                _treeView.SelectedNode.TextColor = ColorResources.Changed;

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

                    _selectedGamePlugin = _previewBox.SelectedItem?.Content;

                    foreach (TranslatedTextEntry translatedEntry in _translatedTextEntries.Where(e => e.Entry.ContentChanged))
                    {
                        translatedEntry.Entry.TextData = translatedEntry.OriginalTextData;
                        translatedEntry.Entry.ContentChanged = false;
                    }

                    _state.FormCommunicator.Update(true, false);

                    ResetTreeState(_treeView.Nodes);

                    await UpdateTextAndPreview();
                }
                else
                {
                    _previewBox.SelectedItemChanged -= _previewBox_SelectedItemChanged;
                    _previewBox.SelectedItem = _previewBox.Items.FirstOrDefault(i => i.Content == _selectedGamePlugin);
                    _previewBox.SelectedItemChanged += _previewBox_SelectedItemChanged;

                    return;
                }
            }
            else
            {
                _serializedOriginalTexts.Clear();
                _parsedTranslatedTexts.Clear();
                _serializedTranslatedTexts.Clear();
                _serializedControlTexts.Clear();

                _previewPages = null;
                _previewPageIndex = -1;

                _selectedGamePlugin = _previewBox.SelectedItem?.Content;

                await UpdateTextAndPreview();
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

        private void ResetTreeState(IList<TreeNode<object>> nodes)
        {
            foreach (TreeNode<object> node in nodes)
            {
                node.TextColor = SixLabors.ImageSharp.Color.Transparent;
                ResetTreeState(node.Nodes);
            }
        }

        private async Task Save(bool saveAs)
        {
            bool isSaved = await _state.FormCommunicator.Save(saveAs);
            if (!isSaved)
                return;

            foreach (TextEntry textEntry in _state.PluginState.Texts)
                textEntry.ContentChanged = false;

            ResetTreeState(_treeView.Nodes);
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

            _renameEntryButton.Enabled = _state.PluginState.CanRenameEntry && _treeView.SelectedNode?.Data is TranslatedTextEntry;
            _addEntryButton.Enabled = _state.PluginState.CanAddEntry;
            _deleteEntryButton.Enabled = _state.PluginState.CanRemoveEntry;
        }

        private async Task UpdateTextAndPreview()
        {
            object? entry = _treeView.SelectedNode?.Data;
            if (entry is null)
                return;

            if (entry is TranslatedTextEntryPage page)
            {
                SetGamePreviewState(page);

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
                SetGamePreviewState(translatedEntry);

                PreprocessEntry(translatedEntry, out string serializedOriginalText, out string serializedTranslatedText,
                    out string serializedControlText, out IList<CharacterData> parsedTranslatedText);

                _origTextEditor.SetText(serializedOriginalText);
                _editTextEditor.SetText(serializedTranslatedText);
                _controlTextEditor.SetText(serializedControlText);

                IList<IList<CharacterData>> allParsedTranslatedTexts = translatedEntry.Page is not null
                    ? GetParsedPageCharacters(translatedEntry.Page)
                    : [parsedTranslatedText];

                _previewPages = await GeneratePreviews(allParsedTranslatedTexts);
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
                        PersistEntry(pageEntry, out serializedOriginalText, out serializedTranslatedText, out serializedControlText, out parsedTranslatedText);
                    else
                        PersistEntry(pageEntry, out _, out _, out _, out _);
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

        private async Task<IList<Image<Rgba32>>?> GeneratePreviews(IList<IList<CharacterData>> parsedTexts)
        {
            if (_selectedGamePlugin is not null)
            {
                _editTextEditor.IsReadOnly = true;
                _treeView.Enabled = false;

                IList<Image<Rgba32>>? previews = await CreatePreviewPages(parsedTexts);

                _editTextEditor.IsReadOnly = false;
                _treeView.Enabled = true;

                return previews;
            }

            FontFamily? fontFamily = _fontFamilyBox.SelectedItem?.Content;
            if (fontFamily is null)
                return null;

            var font = new Font(fontFamily, 15, FontStyle.Regular);
            var glyphProvider = new SystemFontGlyphProvider(font);

            var layouter = new TextLayouter(new LayoutOptions(), glyphProvider);

            IList<IList<TextLayoutLineData>> layoutLines = [];
            foreach (IList<CharacterData> parsedText in parsedTexts)
                layoutLines.Add(layouter.Create(parsedText));

            int imageWidth = layoutLines.Count <= 0 ? 0 : layoutLines.Max(t => t.Count <= 0 ? 0 : t.Max(l => l.BoundingBox.Width));
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

        private void SetGamePreviewState(TranslatedTextEntry currentEntry)
        {
            IList<TextEntry> entries = [currentEntry.Entry];
            if (currentEntry.Page is not null)
                entries = currentEntry.Page.Page.Entries;

            _selectedGameState = CreateGamePreviewState(entries);
        }

        private void SetGamePreviewState(TranslatedTextEntryPage currentPage)
        {
            IList<TextEntry> entries = currentPage.Page.Entries;

            _selectedGameState = CreateGamePreviewState(entries);
        }

        private IGamePluginState? CreateGamePreviewState(IList<TextEntry> entries)
        {
            return _selectedGamePlugin?.CreatePluginState(_state.FileState.FilePath, entries, _fileManager);
        }

        private ICharacterParser GetCharacterParser()
        {
            return _selectedGameState?.TextProcessing?.Parser ?? new CharacterParser();
        }

        private ICharacterSerializer GetCharacterSerializer()
        {
            return _selectedGameState?.TextProcessing?.Serializer ?? new CharacterSerializer();
        }

        private ICharacterComposer GetCharacterComposer()
        {
            return _selectedGameState?.TextProcessing?.Composer ?? new CharacterComposer();
        }

        private ICharacterDeserializer GetCharacterDeserializer()
        {
            return _selectedGameState?.TextProcessing?.Deserializer ?? new CharacterDeserializer();
        }

        private async Task<IList<Image<Rgba32>>?> CreatePreviewPages(IList<IList<CharacterData>> parsedTexts)
        {
            if (_selectedGameState is null || !_selectedGameState.CanProcessTexts)
                return null;

            return await _selectedGameState.TextProcessing!.AttemptRenderPreviews(parsedTexts) ?? null;
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
