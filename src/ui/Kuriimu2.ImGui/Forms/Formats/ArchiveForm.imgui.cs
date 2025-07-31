using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using Konnect.DataClasses.FileSystem;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ArchiveForm
    {
        private StackLayout _mainLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;

        private TextBox _searchBox;
        private ImageButton _clearButton;

        private TreeView<DirectoryEntry> _treeView;
        private DataTable<ArchiveFile> _fileView;

        private Label _fileCount;

        private Button _cancelBtn;

        private ContextMenu _directoryContext;
        private ContextMenu _fileContext;

        private MenuBarButton _extractDirectoryButton;
        private MenuBarButton _replaceDirectoryButton;
        private MenuBarButton _renameDirectoryButton;
        private MenuBarButton _addDirectoryButton;
        private MenuBarButton _deleteDirectoryButton;

        private MenuBarButton _openFileButton;
        private MenuBarMenu _openWithFileMenu;
        private MenuBarButton _extractFileButton;
        private MenuBarButton _replaceFileButton;
        private MenuBarButton _renameFileButton;
        private MenuBarButton _deleteFileButton;

        private void InitializeComponent()
        {
            #region Controls

            _extractDirectoryButton = new MenuBarButton { Text = LocalizationResources.ArchiveDirectoryExtract };
            _replaceDirectoryButton = new MenuBarButton { Text = LocalizationResources.ArchiveDirectoryReplace };
            _renameDirectoryButton = new MenuBarButton { Text = LocalizationResources.ArchiveDirectoryRename };
            _addDirectoryButton = new MenuBarButton { Text = LocalizationResources.ArchiveDirectoryAdd };
            _deleteDirectoryButton = new MenuBarButton
            {
                Text = LocalizationResources.ArchiveDirectoryDelete,
                KeyAction = new KeyCommand(Key.Delete, LocalizationResources.ArchiveDirectoryDeleteShortcut)
            };

            _openFileButton = new MenuBarButton { Text = LocalizationResources.ArchiveFileOpen };
            _openWithFileMenu = new MenuBarMenu { Text = LocalizationResources.ArchiveFileOpenWith };
            _extractFileButton = new MenuBarButton { Text = LocalizationResources.ArchiveFileExtract };
            _replaceFileButton = new MenuBarButton { Text = LocalizationResources.ArchiveFileReplace };
            _renameFileButton = new MenuBarButton { Text = LocalizationResources.ArchiveFileRename };
            _deleteFileButton = new MenuBarButton
            {
                Text = LocalizationResources.ArchiveFileDelete,
                KeyAction = new KeyCommand(Key.Delete, LocalizationResources.ArchiveFileDeleteShortcut)
            };

            _directoryContext = new ContextMenu
            {
                Items =
                {
                    _extractDirectoryButton,
                    _replaceDirectoryButton,
                    _renameDirectoryButton,
                    _addDirectoryButton,
                    _deleteDirectoryButton
                }
            };

            _fileContext = new ContextMenu
            {
                Items =
                {
                    _openFileButton,
                    _openWithFileMenu,
                    new MenuBarSplitter(),
                    _extractFileButton,
                    _replaceFileButton,
                    _renameFileButton,
                    _deleteFileButton
                }
            };

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

            _searchBox = new TextBox { Placeholder = LocalizationResources.ArchiveSearchPlaceholder };
            _clearButton = new ImageButton { Image = ImageResources.Close, Tooltip = LocalizationResources.ArchiveSearchClear };

            _treeView = new TreeView<DirectoryEntry> { ContextMenu = _directoryContext };
            _fileView = new DataTable<ArchiveFile>
            {
                Columns =
                {
                    new DataTableColumn<ArchiveFile>(a => a.Name,LocalizationResources.ArchiveTableFilesName),
                    new DataTableColumn<ArchiveFile>(a => $"{a.Size}",LocalizationResources.ArchiveTableFilesSize)
                },
                CanSelectMultiple = true,
                ContextMenu = _fileContext
            };

            _fileCount = new Label();
            _cancelBtn = new Button { Text = LocalizationResources.ArchiveCancelOperation, Width = 75, Enabled = false };

            #endregion

            #region Main Content

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
                        Size = new Size(1f,-1),
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
                        Items =
                        {
                            new StackLayout
                            {
                                Alignment = Alignment.Vertical,
                                ItemSpacing = 4,
                                Size = new Size(.3f, 1f),
                                Items =
                                {
                                    new StackLayout
                                    {
                                        Alignment = Alignment.Horizontal,
                                        ItemSpacing = 4,
                                        Size = new Size(1f, -1),
                                        Items =
                                        {
                                            _searchBox,
                                            new StackItem(_clearButton) {VerticalAlignment = VerticalAlignment.Center}
                                        }
                                    },
                                    _treeView
                                }
                            },
                            new StackLayout
                            {
                                Alignment = Alignment.Vertical,
                                ItemSpacing = 4,
                                Size = new Size(.7f, 1f),
                                Items =
                                {
                                    _fileView,
                                    new StackLayout
                                    {
                                        Alignment = Alignment.Horizontal,
                                        Size = new Size(1f, -1),
                                        Items =
                                        {
                                            _fileCount,
                                            new StackLayout
                                            {
                                                Alignment = Alignment.Horizontal,
                                                HorizontalAlignment = HorizontalAlignment.Right,
                                                Size = new Size(1f, -1),
                                                Items =
                                                {
                                                    _cancelBtn
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

            #endregion
        }

        protected override void SetTabInactiveCore()
        {
            _treeView.SetTabInactive();
            _fileView.SetTabInactive();
        }
    }
}
