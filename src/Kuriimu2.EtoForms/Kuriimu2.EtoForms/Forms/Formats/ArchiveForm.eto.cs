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

            searchTextBox = new TextBox
            {
                Size=new Size(268,-1)
            };

            #region Commands

            searchClearCommand = new Command { MenuText = "Clear" };

            cancelCommand = new Command { MenuText = "Cancel" };

            openCommand = new Command { MenuText = "Open" };
            openWithCommand = new Command { MenuText = "Open with" };

            saveCommand = new Command { MenuText = "Save", Shortcut = SaveHotKey };
            saveAsCommand = new Command { MenuText = "Save As", Shortcut = SaveAsHotKey };

            extractDirectoryCommand = new Command { MenuText = "Extract", Image = MenuExportResource };
            replaceDirectoryCommand = new Command { MenuText = "Replace", Image = MenuImportResource };
            renameDirectoryCommand = new Command { MenuText = "Rename", Image = MenuEditResource };
            deleteDirectoryCommand = new Command { MenuText = "Delete", Image = MenuDeleteResource };
            addDirectoryCommand = new Command { MenuText = "Add", Image = MenuAddResource };

            extractFileCommand = new Command { MenuText = "Extract", Image = MenuExportResource };
            replaceFileCommand = new Command { MenuText = "Replace", Image = MenuImportResource };
            renameFileCommand = new Command { MenuText = "Rename", Image = MenuEditResource };
            deleteFileCommand = new Command { MenuText = "Delete", Image = MenuDeleteResource };

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

            openWithMenuItem = new ButtonMenuItem { Text = "Open with", Command = openWithCommand };
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
                        HeaderText = "Name",
                        Sortable = true,
                        AutoSize = true
                    },
                    new GridColumn
                    {
                        DataCell = new TextBoxCell(nameof(FileElement.Size)),
                        HeaderText = "Size",
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
                Text = "X",
                Size=new Size(22,-1),
                Command = searchClearCommand
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Command = cancelCommand
            };

            saveButton = new ButtonToolStripItem
            {
                Command = saveCommand,
                Image = MenuSaveResource
            };

            saveAsButton = new ButtonToolStripItem
            {
                Command = saveAsCommand,
                Image = MenuSaveAsResource
            };

            extractButton = new ButtonToolStripItem
            {
                Command = extractFileCommand,
                Image = MenuExportResource
            };

            replaceButton = new ButtonToolStripItem
            {
                Command = replaceFileCommand,
                Image = MenuImportResource
            };

            renameButton = new ButtonToolStripItem
            {
                Command = renameFileCommand,
                Image = MenuExportResource
            };

            deleteButton = new ButtonToolStripItem
            {
                Command = deleteFileCommand,
                Image = MenuDeleteResource
            };

            #endregion

            #endregion

            #region Content

            var archiveToolStrip = new ToolStrip
            {
                BackgroundColor = KnownColors.White,
                Items =
                {
                    saveButton,
                    saveAsButton
                }
            };

            var mainContent = new TableLayout
            {
                Spacing=new Size(3,3),
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
                                Size = new SizeF(-1, ToolStripItem.Height + 6),
                                BackgroundColor = KnownColors.White,
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
                    new TableRow(new Panel { Content = archiveToolStrip, Size = new Size(-1, (int)ToolStripItem.Height + 6) }),
                    new TableRow { Cells = { new TableCell(mainContent) { ScaleWidth = true } }, ScaleHeight = true }
                }
            };

            #endregion
        }
    }
}
