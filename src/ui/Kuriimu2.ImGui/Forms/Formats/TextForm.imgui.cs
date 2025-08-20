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
using Konnect.Contract.DataClasses.Plugin.File.Text;
using Konnect.Contract.Plugin.Game;
using Veldrid;
using Rectangle = Veldrid.Rectangle;
using Size = ImGui.Forms.Models.Size;
using Kuriimu2.ImGui.Models.Forms.Formats;

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
        private ComboBox<IGamePluginState?> _previewBox;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private ArrowButton _previousPageBtn;
        private ArrowButton _nextPageBtn;

        private IGamePluginState? _selectedPreviewPlugin;
        private readonly List<TranslatedTextEntry> _translatedTextEntries = [];

        private void InitializeComponent()
        {
            #region Controls

            _treeView = new TreeView<object> { Size = new Size(.2f, SizeValue.Parent) };

            _origTextEditor = new TextEditor { IsReadOnly = true };
            _editTextEditor = new TextEditor();
            _controlTextEditor = new TextEditor { IsReadOnly = true };
            _textPreview = new ZoomablePictureBox { ShowBorder = true };

            _fontFamilyBox = new ComboBox<FontFamily>();
            _previewBox = new ComboBox<IGamePluginState?>();

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

            _previousPageBtn = new ArrowButton(ImGuiDir.Left) { KeyAction = new(Key.Left) };
            _nextPageBtn = new ArrowButton(ImGuiDir.Right) { KeyAction = new(Key.Right) };

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
                            _saveAsBtn
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
                                            _origTextEditor,
                                            _editTextEditor
                                        }
                                    },
                                    new TableRow
                                    {
                                        Cells =
                                        {
                                            _controlTextEditor,
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
            IReadOnlyList<Guid> preferredGamePluginIds = _state.PluginState.Previews ?? [];
            IGamePlugin[] gamePlugins = _pluginManager.GetPlugins<IGamePlugin>().ToArray();

            foreach (Guid preferredGamePluginId in preferredGamePluginIds)
            {
                IGamePlugin? preferredGamePlugin = gamePlugins.FirstOrDefault(x => x.PluginId == preferredGamePluginId);
                if (preferredGamePlugin is null)
                    continue;

                var dropDownItem = new DropDownItem<IGamePluginState?>(preferredGamePlugin.CreatePluginState(_fileManager), preferredGamePlugin.Metadata.Name);
                _previewBox.Items.Add(dropDownItem);
                _previewBox.PreferredItems.Add(dropDownItem);
            }

            _previewBox.Items.Add(new DropDownItem<IGamePluginState?>(null, LocalizationResources.TextPreviewDefault));

            foreach (IGamePlugin gamePlugin in gamePlugins.ExceptBy(preferredGamePluginIds, g => g.PluginId))
                _previewBox.Items.Add(new DropDownItem<IGamePluginState?>(gamePlugin.CreatePluginState(_fileManager), gamePlugin.Metadata.Name));

            if (_previewBox.Items.Count > 0)
            {
                _previewBox.SelectedItem = _previewBox.Items[0];
                _selectedPreviewPlugin = _previewBox.SelectedItem.Content;
            }
        }

        private void InitializeTexts()
        {
            var pager = _state.PluginState.Pager;
            if (pager is not null)
            {
                var pages = pager.Page(_state.PluginState.Texts);
                for (var i = 0; i < pages.Length; i++)
                {
                    TranslatedTextEntryPage translatedPage = AddTranslatedPage(pages[i], _treeView.Nodes);
                    _translatedTextEntries.AddRange(translatedPage.Entries);
                }
            }
            else
            {
                for (var i = 0; i < _state.PluginState.Texts.Count; i++)
                {
                    TranslatedTextEntry translatedEntry = AddTranslatedEntry(_state.PluginState.Texts[i], i, null, _treeView.Nodes);
                    _translatedTextEntries.Add(translatedEntry);
                }
            }

            if (_state.PluginState.Texts.Count > 0)
                _treeView.SelectedNode = _treeView.Nodes[0];
        }

        private static TranslatedTextEntryPage AddTranslatedPage(TextEntryPage page, IList<TreeNode<object>> nodes)
        {
            var translatedPage = new TranslatedTextEntryPage
            {
                Entries = new List<TranslatedTextEntry>(),
                Page = page
            };

            var node = new TreeNode<object>
            {
                Text = page.Name,
                Data = translatedPage,
                IsExpanded = true
            };

            for (var i = 0; i < page.Entries.Count; i++)
            {
                AddTranslatedEntry(page.Entries[i], i, translatedPage, node.Nodes);
            }

            nodes.Add(node);

            return translatedPage;
        }

        private static TranslatedTextEntry AddTranslatedEntry(TextEntry entry, int index, TranslatedTextEntryPage? page, IList<TreeNode<object>> nodes)
        {
            var translatedEntry = new TranslatedTextEntry
            {
                Page = page,
                Entry = entry,
                OriginalTextData = entry.TextData
            };

            var node = new TreeNode<object>
            {
                Text = entry.Name ?? $"no_name_{index:00}",
                Data = translatedEntry
            };

            page?.Entries.Add(translatedEntry);

            nodes.Add(node);

            return translatedEntry;
        }

        #region Component implementation

        public override Size GetSize()
        {
            return Size.Parent;
        }

        protected override void UpdateInternal(Rectangle contentRect)
        {
            _mainLayout.Update(contentRect);
        }

        #endregion
    }
}
