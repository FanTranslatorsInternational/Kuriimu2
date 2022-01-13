using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Controls.Tree;
using ImGui.Forms.Models;
using Komponent.Extensions;
using Kuriimu2.ImGui.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ArchiveForm
    {
        private StackLayout _mainLayout;

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

            _extractDirectoryButton = new MenuBarButton { Caption = LocalizationResources.ExtractCaptionResource() };
            _replaceDirectoryButton = new MenuBarButton { Caption = LocalizationResources.ReplaceCaptionResource() };
            _renameDirectoryButton = new MenuBarButton { Caption = LocalizationResources.RenameCaptionResource() };
            _addDirectoryButton = new MenuBarButton { Caption = LocalizationResources.AddCaptionResource() };
            _deleteDirectoryButton = new MenuBarButton { Caption = LocalizationResources.DeleteCaptionResource() };

            _openFileButton = new MenuBarButton { Caption = LocalizationResources.OpenResource() };
            _openWithFileMenu = new MenuBarMenu { Caption = LocalizationResources.OpenWithResource() };
            _extractFileButton = new MenuBarButton { Caption = LocalizationResources.ExtractCaptionResource() };
            _replaceFileButton = new MenuBarButton { Caption = LocalizationResources.ReplaceCaptionResource() };
            _renameFileButton = new MenuBarButton { Caption = LocalizationResources.RenameCaptionResource() };
            _deleteFileButton = new MenuBarButton { Caption = LocalizationResources.DeleteCaptionResource() };

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

            _searchBox = new TextBox { Placeholder = LocalizationResources.SearchCaptionResource() };
            _clearButton = new ImageButton { Image = ImageResources.Close };

            _treeView = new TreeView<DirectoryEntry> { ContextMenu = _directoryContext };
            _fileView = new DataTable<ArchiveFile>
            {
                Columns =
                {
                    new DataTableColumn<ArchiveFile>(a => a.Name,LocalizationResources.FileNameCaptionResource()),
                    new DataTableColumn<ArchiveFile>(a => $"{a.Size}",LocalizationResources.FileSizeCaptionResource())
                },
                ContextMenu = _fileContext
            };

            _fileCount = new Label();
            _cancelBtn = new Button { Caption = LocalizationResources.CancelOperationResource(), Width = 75, Enabled = false };

            #endregion

            #region Main Content

            _mainLayout = new StackLayout
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
                                Size = new Size(1f,-1),
                                Items =
                                {
                                    _fileCount,
                                    new StackLayout
                                    {
                                        Alignment = Alignment.Horizontal,
                                        HorizontalAlignment = HorizontalAlignment.Right,
                                        Size = new Size(1f,-1),
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
            };

            #endregion
        }
    }
}
