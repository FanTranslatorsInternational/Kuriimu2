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

            searchClearCommand = new Command { MenuText = "Clear", Image = ImageResources.Actions.Delete };

            cancelCommand = new Command { MenuText = "Cancel" };

            openCommand = new Command { MenuText = "Open", Image = ImageResources.Actions.Open };
            openWithCommand = new Command { MenuText = "Open with", Image = ImageResources.Actions.OpenWith };

            saveCommand = new Command { MenuText = "Save", Shortcut = SaveHotKey, Image = ImageResources.Actions.Save };
            saveAsCommand = new Command { MenuText = "Save As", Shortcut = SaveAsHotKey, Image = ImageResources.Actions.SaveAs };

            extractDirectoryCommand = new Command { MenuText = "Extract", Image = ImageResources.Actions.FolderExport };
            replaceDirectoryCommand = new Command { MenuText = "Replace", Image = ImageResources.Actions.FolderImport };
            renameDirectoryCommand = new Command { MenuText = "Rename", Image = ImageResources.Actions.Rename };
            deleteDirectoryCommand = new Command { MenuText = "Delete", Image = ImageResources.Actions.Delete };
            addDirectoryCommand = new Command { MenuText = "Add", Image = ImageResources.Actions.Add };

            extractFileCommand = new Command { MenuText = "Extract", Image = ImageResources.Actions.FileExport };
            replaceFileCommand = new Command { MenuText = "Replace", Image = ImageResources.Actions.FileImport };
            renameFileCommand = new Command { MenuText = "Rename", Image = ImageResources.Actions.Rename };
            deleteFileCommand = new Command { MenuText = "Delete", Image = ImageResources.Actions.Delete };

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
            openWithMenuItem = new ButtonMenuItem { Text = "Open with", Command = openWithCommand, Image = openWithCommand.Image };
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
                Image = ImageResources.Actions.Clear,
                Command = searchClearCommand,
                Size = new Size(22,-1)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Command = cancelCommand
            };

            saveButton = new ButtonToolStripItem
            {
                Command = saveCommand,
            };

            saveAsButton = new ButtonToolStripItem
            {
                Command = saveAsCommand,
            };

            extractButton = new ButtonToolStripItem
            {
                Command = extractFileCommand,
            };

            replaceButton = new ButtonToolStripItem
            {
                Command = replaceFileCommand,
            };

            renameButton = new ButtonToolStripItem
            {
                Command = renameFileCommand,
            };

            deleteButton = new ButtonToolStripItem
            {
                Command = deleteFileCommand,
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
