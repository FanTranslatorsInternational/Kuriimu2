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

            saveCommand = new Command { MenuText = "Save" };
            saveAsCommand = new Command { MenuText = "Save As" };

            extractDirectoryCommand = new Command { MenuText = "Extract" };
            replaceDirectoryCommand = new Command { MenuText = "Replace" };
            renameDirectoryCommand = new Command { MenuText = "Rename" };
            deleteDirectoryCommand = new Command { MenuText = "Delete" };

            extractFileCommand = new Command { MenuText = "Extract" };
            replaceFileCommand = new Command { MenuText = "Replace" };
            renameFileCommand = new Command { MenuText = "Rename" };
            deleteFileCommand = new Command { MenuText = "Delete" };

            #endregion

            #region Folders

            var folderContext = new ContextMenu
            {
                Items =
                {
                    extractDirectoryCommand,
                    replaceDirectoryCommand,
                    renameDirectoryCommand,
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

            openWithMenuItem = new ButtonMenuItem { Command=openWithCommand };
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
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save.png")
            };

            saveAsButton = new ButtonToolStripItem
            {
                Command = saveAsCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save-as.png")
            };

            extractButton = new ButtonToolStripItem
            {
                Command = extractFileCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-export.png")
            };

            replaceButton = new ButtonToolStripItem
            {
                Command = replaceFileCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-import.png")
            };

            renameButton = new ButtonToolStripItem
            {
                Command = renameFileCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-edit.png")
            };

            deleteButton = new ButtonToolStripItem
            {
                Command = deleteFileCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-delete.png")
            };

            #endregion

            #endregion

            Content = new FixedSplitter((int)ToolStripItem.Height + 6)
            {
                Orientation = Orientation.Vertical,

                Panel1 = new ToolStrip
                {
                    BackgroundColor = KnownColors.White,
                    Items =
                    {
                        saveButton,
                        saveAsButton
                    }
                },
                Panel2 = new FixedSplitter(300)
                {
                    Panel1 = new FixedSplitter(30)
                    {
                        Orientation = Orientation.Vertical,

                        Panel1 = new StackLayout
                        {
                            Orientation = Orientation.Horizontal,

                            Padding=new Padding(3),
                            Spacing=3,

                            Items =
                            {
                                searchTextBox,
                                searchClearButton
                            }
                        },
                        Panel2 = folderView
                    },
                    Panel2 = new FixedSplitter((int)ToolStripItem.Height + 6)
                    {
                        Orientation = Orientation.Vertical,

                        Panel1 = new ToolStrip {
                            BackgroundColor = KnownColors.White,
                            Items =
                            {
                                extractButton,
                                replaceButton,
                                renameButton,
                                deleteButton
                            }
                        },
                        Panel2 = fileView
                    }
                }
            };
        }
    }
}
