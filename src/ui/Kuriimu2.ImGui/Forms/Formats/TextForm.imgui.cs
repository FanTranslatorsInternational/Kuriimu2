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

        private TreeView<TranslatedTextEntry> _treeView;

        private TextEditor _origTextEditor;
        private TextEditor _editTextEditor;
        private TextEditor _controlTextEditor;
        private ZoomablePictureBox _textPreview;

        private ComboBox<FontFamily> _fontFamilyBox;
        private ComboBox<IGamePluginState> _previewBox;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;

        private void InitializeComponent()
        {
            #region Controls

            _treeView = new TreeView<TranslatedTextEntry> { Size = new Size(.2f, SizeValue.Parent) };

            _origTextEditor = new TextEditor { IsReadOnly = true };
            _editTextEditor = new TextEditor();
            _controlTextEditor = new TextEditor { IsReadOnly = true };
            _textPreview = new ZoomablePictureBox { ShowBorder = true };

            _fontFamilyBox = new ComboBox<FontFamily>();
            _previewBox = new ComboBox<IGamePluginState>();

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
                                                    _previewBox,
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

                _previewBox.Items.Add(new DropDownItem<IGamePluginState>(preferredGamePlugin.CreatePluginState(_fileManager), preferredGamePlugin.Metadata.Name));
            }

            foreach (IGamePlugin gamePlugin in gamePlugins.ExceptBy(preferredGamePluginIds, g => g.PluginId))
                _previewBox.Items.Add(new DropDownItem<IGamePluginState>(gamePlugin.CreatePluginState(_fileManager), gamePlugin.Metadata.Name));

            if (_previewBox.Items.Count > 0)
                _previewBox.SelectedItem = _previewBox.Items[0];
        }

        private void InitializeTexts()
        {
            for (var i = 0; i < _state.PluginState.Texts.Count; i++)
            {
                TextEntry textEntry = _state.PluginState.Texts[i];
                var node = new TreeNode<TranslatedTextEntry>
                {
                    Text = textEntry.Name ?? $"no_name_{i:00}",
                    Data = new TranslatedTextEntry
                    {
                        Entry = textEntry,
                        OriginalTextData = textEntry.TextData
                    }
                };

                _treeView.Nodes.Add(node);
            }

            if (_state.PluginState.Texts.Count > 0)
                _treeView.SelectedNode = _treeView.Nodes[0];
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
