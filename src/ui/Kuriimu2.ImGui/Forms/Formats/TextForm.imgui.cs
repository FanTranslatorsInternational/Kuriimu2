using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Base;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;
using System.Numerics;
using ImGui.Forms.Controls.Text.Editor;
using ImGuiNET;
using Konnect.Contract.Plugin.Game;
using Veldrid;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;
using Kuriimu2.ImGui.Models.Forms.Formats;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models.IO;
using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class TextForm : Component
    {
        private StackLayout _mainLayout;

        private TreeView<object> _treeView;

        private TextEditor _origTextEditor;
        private TextEditor _editTextEditor;
        private TextEditor _controlTextEditor;
        private ZoomablePictureBox _textPreview;

        private ComboBox<FontFamily> _fontFamilyBox;
        private ComboBox<IGamePlugin?> _previewBox;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private ImageButton _poExportBtn;
        private ImageButton _poImportBtn;
        private ImageButton _kupExportBtn;
        private ImageButton _kupImportBtn;

        private ArrowButton _previousPageBtn;
        private ArrowButton _nextPageBtn;

        private ContextMenu _entryContext;

        private MenuBarButton _renameEntryButton;
        private MenuBarButton _addEntryButton;
        private MenuBarButton _deleteEntryButton;

        private IGamePlugin? _selectedGamePlugin;
        private IGamePluginState? _selectedGameState;
        private readonly List<TranslatedTextEntry> _translatedTextEntries = [];
        private readonly Dictionary<TranslatedTextEntry, TreeNode<object>> _translatedTextEntryNodes = [];

        private readonly Dictionary<string, int> _entryNameLookup = [];
        private readonly Dictionary<string, int> _pageNameLookup = [];

        private void InitializeComponent()
        {
            #region Controls

            _origTextEditor = new TextEditor { IsReadOnly = true };
            _editTextEditor = new TextEditor();
            _controlTextEditor = new TextEditor { IsReadOnly = true };
            _textPreview = new ZoomablePictureBox { ShowBorder = true };

            _fontFamilyBox = new ComboBox<FontFamily>();
            _previewBox = new ComboBox<IGamePlugin?>();

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
            _poExportBtn = new ImageButton { Image = ImageResources.PoExport, Tooltip = LocalizationResources.TextMenuExportPo, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _poImportBtn = new ImageButton { Image = ImageResources.PoImport, Tooltip = LocalizationResources.TextMenuImportPo, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _kupExportBtn = new ImageButton { Image = ImageResources.KupExport, Tooltip = LocalizationResources.TextMenuExportKup, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _kupImportBtn = new ImageButton { Image = ImageResources.KupImport, Tooltip = LocalizationResources.TextMenuImportKup, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };

            _previousPageBtn = new ArrowButton(ImGuiDir.Left) { KeyAction = new(Key.Left) };
            _nextPageBtn = new ArrowButton(ImGuiDir.Right) { KeyAction = new(Key.Right) };

            _renameEntryButton = new MenuBarButton { Text = LocalizationResources.TextContextRename };
            _addEntryButton = new MenuBarButton { Text = LocalizationResources.TextContextAdd };
            _deleteEntryButton = new MenuBarButton
            {
                Text = LocalizationResources.TextContextDelete,
                KeyAction = new KeyCommand(Key.Delete, LocalizationResources.TextContextDeleteShortcut)
            };

            _entryContext = new ContextMenu
            {
                Items =
                {
                    _renameEntryButton,
                    _addEntryButton,
                    _deleteEntryButton
                }
            };

            _treeView = new TreeView<object> { Size = new Size(.2f, SizeValue.Parent), ContextMenu = _entryContext };

            #endregion

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 4,
                        Size = Size.WidthAlign,
                        Items =
                        {
                            _saveBtn,
                            _saveAsBtn,
                            new Splitter { Length = 26, Alignment = Alignment.Vertical },
                            _poExportBtn,
                            _poImportBtn,
                            new Splitter { Length = 26, Alignment = Alignment.Vertical },
                            _kupExportBtn,
                            _kupImportBtn
                        }
                    },
                    new StackLayout
                    {
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 4,
                        Size = Size.WidthAlign,
                        Items =
                        {
                            _treeView,
                            new TableLayout
                            {
                                Size = new Size(.8f, SizeValue.Parent),
                                Spacing = new(4, 4),
                                Rows =
                                {
                                    new TableRow
                                    {
                                        Cells =
                                        {
                                            new StackLayout
                                            {
                                                Alignment = Alignment.Vertical,
                                                ItemSpacing = 4,
                                                Items =
                                                {
                                                    new Label(LocalizationResources.TextContentOriginal),
                                                    _origTextEditor
                                                }
                                            },
                                            new StackLayout
                                            {
                                                Alignment = Alignment.Vertical,
                                                ItemSpacing = 4,
                                                Items =
                                                {
                                                    new Label(LocalizationResources.TextContentEdited),
                                                    _editTextEditor
                                                }
                                            }
                                        }
                                    },
                                    new TableRow
                                    {
                                        Cells =
                                        {
                                            new StackLayout
                                            {
                                                Alignment = Alignment.Vertical,
                                                ItemSpacing = 4,
                                                Items =
                                                {
                                                    new Label(LocalizationResources.TextContentNoCodes),
                                                    _controlTextEditor
                                                }
                                            },
                                            new StackLayout
                                            {
                                                Alignment = Alignment.Vertical,
                                                ItemSpacing = 4,
                                                Items =
                                                {
                                                    new StackLayout
                                                    {
                                                        Alignment = Alignment.Horizontal,
                                                        Size = Size.WidthAlign,
                                                        ItemSpacing = 4,
                                                        Items =
                                                        {
                                                            _previewBox,
                                                            new StackItem(_previousPageBtn){Size = Size.WidthAlign,HorizontalAlignment = HorizontalAlignment.Right},
                                                            _nextPageBtn
                                                        }
                                                    },
                                                    _textPreview
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            InitializeFonts();
            InitializePreviewPlugins();
            InitializeTexts();
        }

        private void InitializeFonts()
        {
            foreach (FontFamily fontFamily in FontFamily.Families)
                _fontFamilyBox.Items.Add(new DropDownItem<FontFamily>(fontFamily, fontFamily.Name));

            if (_fontFamilyBox.Items.Count > 0)
                _fontFamilyBox.SelectedItem = _fontFamilyBox.Items[0];
        }

        private void InitializePreviewPlugins()
        {
            IReadOnlyList<Guid> preferredGamePluginIds = _state.PluginState.PreviewGuids ?? [];
            IGamePlugin[] gamePlugins = _pluginManager.GetPlugins<IGamePlugin>().ToArray();

            foreach (Guid preferredGamePluginId in preferredGamePluginIds)
            {
                IGamePlugin? preferredGamePlugin = gamePlugins.FirstOrDefault(x => x.PluginId == preferredGamePluginId);
                if (preferredGamePlugin is null)
                    continue;

                var dropDownItem = new DropDownItem<IGamePlugin?>(preferredGamePlugin, preferredGamePlugin.Metadata.Name);
                _previewBox.Items.Add(dropDownItem);
                _previewBox.PreferredItems.Add(dropDownItem);
            }

            _previewBox.Items.Add(new DropDownItem<IGamePlugin?>(null, LocalizationResources.TextPreviewDefault));

            foreach (IGamePlugin gamePlugin in gamePlugins.ExceptBy(preferredGamePluginIds, g => g.PluginId))
                _previewBox.Items.Add(new DropDownItem<IGamePlugin?>(gamePlugin, gamePlugin.Metadata.Name));

            if (_previewBox.Items.Count > 0)
            {
                _previewBox.SelectedItem = _previewBox.Items[0];
                _selectedGamePlugin = _previewBox.SelectedItem.Content;
            }
        }

        private void InitializeTexts()
        {
            object[] entries = CreateTranslatedPagedEntries();

            foreach (object entry in entries)
            {
                switch (entry)
                {
                    case TranslatedTextEntryPage page:
                        AddTranslatedPage(page, _treeView.Nodes);
                        break;

                    case TranslatedTextEntry translatedEntry:
                        AddTranslatedEntry(translatedEntry, _treeView.Nodes);
                        break;
                }
            }

            if (_state.PluginState.Texts.Count > 0)
                _treeView.SelectedNode = _treeView.Nodes[0];
        }

        private void UpdateNames()
        {
            _pageNameLookup.Clear();
            _entryNameLookup.Clear();

            var index = 0;
            foreach (TreeNode<object> node in _treeView.Nodes)
            {
                switch (node.Data)
                {
                    case TranslatedTextEntryPage page:
                        node.Text = page.Name = CreatePageName(page.Page, index++);

                        for (var i = 0; i < node.Nodes.Count; i++)
                        {
                            var translatedEntry = (TranslatedTextEntry)node.Nodes[i].Data;
                            node.Nodes[i].Text = translatedEntry.Name = CreateEntryName(translatedEntry.Entry, i);
                        }
                        break;

                    case TranslatedTextEntry entry:
                        node.Text = entry.Name = CreateEntryName(entry.Entry, index++);
                        break;
                }
            }
        }

        private object[] CreateTranslatedPagedEntries()
        {
            var result = new List<object>();

            var pager = _state.PluginState.Pager;
            if (pager is not null)
            {
                var pages = pager.Page(_state.PluginState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    string pageName = CreatePageName(pages[i], i);

                    var translatedPage = new TranslatedTextEntryPage
                    {
                        Page = pages[i],
                        Name = pageName,
                        Entries = new List<TranslatedTextEntry>()
                    };

                    for (var j = 0; j < pages[i].Entries.Count; j++)
                    {
                        string entryName = CreateEntryName(pages[i].Entries[j], j);

                        var translatedEntry = new TranslatedTextEntry
                        {
                            Page = translatedPage,
                            Entry = pages[i].Entries[j],
                            Name = entryName,
                            OriginalTextData = pages[i].Entries[j].TextData
                        };

                        translatedPage.Entries.Add(translatedEntry);
                    }

                    result.Add(translatedPage);
                }
            }
            else
            {
                for (var i = 0; i < _state.PluginState.Texts.Count; i++)
                {
                    string entryName = CreateEntryName(_state.PluginState.Texts[i], i);

                    var translatedEntry = new TranslatedTextEntry
                    {
                        Page = null,
                        Entry = _state.PluginState.Texts[i],
                        Name = entryName,
                        OriginalTextData = _state.PluginState.Texts[i].TextData
                    };

                    result.Add(translatedEntry);
                }
            }

            return [.. result];
        }

        private string CreatePageName(TextEntryPage page, int index)
        {
            if (page.Name is null)
                return $"no_name_{index:00}";

            string pageName = page.Name;

            if (!_pageNameLookup.TryGetValue(pageName, out int count))
                _pageNameLookup[pageName] = 1;
            else
            {
                _pageNameLookup[pageName]++;
                pageName += $"_{count}";
            }

            return pageName;
        }

        private string CreateEntryName(TextEntry entry, int index)
        {
            if (entry.Name is null)
                return $"no_name_{index:00}";

            string entryName = entry.Name;

            if (!_entryNameLookup.TryGetValue(entryName, out int count))
                _entryNameLookup[entryName] = 1;
            else
            {
                _entryNameLookup[entryName]++;
                entryName += $"_{count}";
            }

            return entryName;
        }

        private void AddTranslatedPage(TranslatedTextEntryPage translatedPage, IList<TreeNode<object>> nodes)
        {
            var node = new TreeNode<object>
            {
                Text = translatedPage.Name,
                Data = translatedPage,
                IsExpanded = true
            };

            foreach (TranslatedTextEntry entry in translatedPage.Entries)
                AddTranslatedEntry(entry, node.Nodes);

            nodes.Add(node);
        }

        private void AddTranslatedEntry(TranslatedTextEntry translatedEntry, IList<TreeNode<object>> nodes)
        {
            var node = new TreeNode<object>
            {
                Text = translatedEntry.Name,
                Data = translatedEntry
            };

            nodes.Add(node);

            _translatedTextEntries.Add(translatedEntry);
            _translatedTextEntryNodes[translatedEntry] = node;
        }

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);

            if (DeleteCommand.IsPressed())
                DeleteSelectedEntry();
        }

        #endregion
    }
}
