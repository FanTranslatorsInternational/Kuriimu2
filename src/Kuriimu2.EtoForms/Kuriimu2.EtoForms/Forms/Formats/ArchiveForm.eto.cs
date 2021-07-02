using System.Collections.ObjectModel;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ArchiveForm : Panel
    {
        #region Localization Keys

        private const string OpenKey_ = "Open";
        private const string OpenWithKey_ = "OpenWith";
        private const string SaveKey_ = "Save";
        private const string SaveAsKey_ = "SaveAs";

        private const string ClearSearchKey_ = "ClearSearch";
        private const string CancelOperationKey_ = "CancelOperation";

        private const string ExtractKey_ = "Extract";
        private const string ReplaceKey_ = "Replace";
        private const string RenameKey_ = "Rename";
        private const string DeleteKey_ = "Delete";
        private const string AddKey_ = "Add";

        private const string ExtractFileKey_ = "ExtractFiles";
        private const string ReplaceFileKey_ = "ReplaceFiles";
        private const string RenameFileKey_ = "RenameFile";
        private const string DeleteFileKey_ = "DeleteFile";

        private const string FileNameKey_ = "FileName";
        private const string FileSizeKey_ = "FileSize";

        #endregion

        #region Controls

        private TextBox searchTextBox;
        private Button searchClearButton;

        private Button cancelButton;

        private TreeGridView folderView;
        private TreeGridItemCollection folders;

        private GridView<FileElement> fileView;
        private ObservableCollection<FileElement> files;

        private ButtonToolStripItem saveButton;
        private ButtonToolStripItem saveAsButton;

        private ButtonToolStripItem extractButton;
        private ButtonToolStripItem replaceButton;
        private ButtonToolStripItem renameButton;
        private ButtonToolStripItem deleteButton;

        private ButtonMenuItem openWithMenuItem;

        #endregion

        #region Commands

        private Command searchClearCommand;

        private Command cancelCommand;

        private Command openCommand;
        private Command openWithCommand;

        private Command saveCommand;
        private Command saveAsCommand;

        private Command extractDirectoryCommand;
        private Command replaceDirectoryCommand;
        private Command renameDirectoryCommand;
        private Command deleteDirectoryCommand;
        private Command addDirectoryCommand;

        private Command extractFileCommand;
        private Command replaceFileCommand;
        private Command renameFileCommand;
        private Command deleteFileCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Initialization

            searchTextBox = new SearchBox
            {
                Size = new Size(268,-1)
            };

            #region Commands

            searchClearCommand = new Command { MenuText = Localize(ClearSearchKey_), Image = ImageResources.Actions.Delete };

            cancelCommand = new Command { MenuText = Localize(CancelOperationKey_) };

            openCommand = new Command { MenuText = Localize(OpenKey_), Image = ImageResources.Actions.Open };
            openWithCommand = new Command { MenuText = Localize(OpenWithKey_), Image = ImageResources.Actions.OpenWith };

            saveCommand = new Command { MenuText = Localize(SaveKey_), Shortcut = SaveHotKey, Image = ImageResources.Actions.Save };
            saveAsCommand = new Command { MenuText = Localize(SaveAsKey_), Shortcut = SaveAsHotKey, Image = ImageResources.Actions.SaveAs };

            extractDirectoryCommand = new Command { MenuText = Localize(ExtractKey_), Image = ImageResources.Actions.FolderExport };
            replaceDirectoryCommand = new Command { MenuText = Localize(ReplaceKey_), Image = ImageResources.Actions.FolderImport };
            renameDirectoryCommand = new Command { MenuText = Localize(RenameKey_), Image = ImageResources.Actions.Rename };
            deleteDirectoryCommand = new Command { MenuText = Localize(DeleteKey_), Image = ImageResources.Actions.Delete };
            addDirectoryCommand = new Command { MenuText = Localize(AddKey_), Image = ImageResources.Actions.Add };

            extractFileCommand = new Command { MenuText = Localize(ExtractKey_), Image = ImageResources.Actions.FileExport };
            replaceFileCommand = new Command { MenuText = Localize(ReplaceKey_), Image = ImageResources.Actions.FileImport };
            renameFileCommand = new Command { MenuText = Localize(RenameKey_), Image = ImageResources.Actions.Rename };
            deleteFileCommand = new Command { MenuText = Localize(DeleteKey_), Image = ImageResources.Actions.Delete };

            #endregion

            #region Folders

            var folderContext = new ContextMenu
            {
                Items =
                {
                    extractDirectoryCommand,
                    replaceDirectoryCommand,
                    renameDirectoryCommand,
                    addDirectoryCommand,
                    deleteDirectoryCommand
                }
            };

            folders = new TreeGridItemCollection();
            folderView = new TreeGridView
            {
                ContextMenu=folderContext,

                DataStore = folders,
                Columns =
                {
                    new GridColumn
                    {
                        DataCell = new ImageTextCell(0, 1)
                    }
                },
                AllowColumnReordering = false
            };

            #endregion

            #region Files

            //NOTE Image has to be set explicitly, I think the Command is not used anymore as soon as sub-items are added
            openWithMenuItem = new ButtonMenuItem { Text = Localize(OpenWithKey_), Command = openWithCommand, Image = openWithCommand.Image };
            var fileContext = new ContextMenu
            {
                Items =
                {
                    openCommand,
                    openWithMenuItem,

                    new SeparatorMenuItem(),

                    extractFileCommand,
                    replaceFileCommand,
                    renameFileCommand,
                    deleteFileCommand
                }
            };

            files = new ObservableCollection<FileElement>();
            fileView = new GridView<FileElement>
            {
                ShowHeader = true,
                AllowMultipleSelection=true,
                BackgroundColor = KnownColors.White,

                ContextMenu=fileContext,

                Columns =
                {
                    new GridColumn
                    {
                        DataCell = new TextBoxCell(nameof(FileElement.Name)),
                        HeaderText = Localize(FileNameKey_),
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        DataCell = new TextBoxCell(nameof(FileElement.Size)),
                        HeaderText = Localize(FileSizeKey_),
                        Sortable = true,
                        AutoSize = true
                    }
                },

                DataStore = files
            };

            #endregion

            #region Buttons

            searchClearButton = new Button
            {
                Image = ImageResources.Actions.Clear,
                ToolTip = Localize(ClearSearchKey_),
                Command = searchClearCommand,
                Size = new Size(22,-1)
            };

            cancelButton = new Button
            {
                Text = Localize(CancelOperationKey_),
                Command = cancelCommand
            };

            saveButton = new ButtonToolStripItem
            {
                ToolTip = Localize(SaveKey_),
                Command = saveCommand,
            };

            saveAsButton = new ButtonToolStripItem
            {
                ToolTip = Localize(SaveAsKey_),
                Command = saveAsCommand,
            };

            extractButton = new ButtonToolStripItem
            {
                ToolTip = Localize(ExtractFileKey_),
                Command = extractFileCommand,
            };

            replaceButton = new ButtonToolStripItem
            {
                ToolTip = Localize(ReplaceFileKey_),
                Command = replaceFileCommand,
            };

            renameButton = new ButtonToolStripItem
            {
                ToolTip = Localize(RenameFileKey_),
                Command = renameFileCommand,
            };

            deleteButton = new ButtonToolStripItem
            {
                ToolTip = Localize(DeleteFileKey_),
                Command = deleteFileCommand,
            };

            #endregion

            #endregion

            #region Content

            var archiveToolStrip = new ToolStrip
            {
                Padding = 3,
                Items =
                {
                    saveButton,
                    saveAsButton
                }
            };

            var mainContent = new TableLayout
            {
                Spacing = new Size(3,3),
                Rows =
                {
                    // Searchbar and file toolstrip
                    new TableRow
                    {
                        Cells =
                        {
                            // Searchbar
                            new StackLayout
                            {
                                Spacing=3,
                                Orientation = Orientation.Horizontal,
                                Items =
                                {
                                    searchTextBox,
                                    searchClearButton
                                }
                            },

                            // file toolstrip
                            new ToolStrip
                            {
                                Items =
                                {
                                    extractButton,
                                    replaceButton,
                                    renameButton,
                                    deleteButton
                                }
                            },
                        }
                    },

                    // Folder and file view
                    new TableRow
                    {
                        Cells =
                        {
                            folderView,
                            fileView
                        }
                    }
                }
            };

            Content = new TableLayout
            {
                Spacing = new Size(3, 3),
                Rows =
                {
                    new TableRow(archiveToolStrip),
                    new TableRow { Cells = { new TableCell(mainContent) { ScaleWidth = true } }, ScaleHeight = true }
                }
            };

            #endregion
        }
    }
}
