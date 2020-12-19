using System.Collections.ObjectModel;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ArchiveForm : Panel
    {
        private ITreeGridStore<ITreeGridItem> folders;
        private ObservableCollection<object> files;

        private ButtonToolStripItem saveButton;
        private ButtonToolStripItem saveAsButton;

        #region Commands

        private Command saveCommand;
        private Command saveAsCommand;

        #endregion

        private void InitializeComponent()
        {
            #region Commands

            saveCommand = new Command { MenuText = "Save" };
            saveAsCommand = new Command { MenuText = "Save As" };

            #endregion

            folders = new TreeGridItemCollection();
            files = new ObservableCollection<object>();

            saveButton = new ButtonToolStripItem
            {
                Command = saveCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save.png"),
                Enabled = false
            };

            saveAsButton = new ButtonToolStripItem
            {
                Command = saveAsCommand,
                Image = Bitmap.FromResource("Kuriimu2.EtoForms.Images.menu-save-as.png")
            };

            Content = new FixedSplitter((int)ToolStripItem.Height + 6)
            {
                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel1,

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
                    Orientation = Orientation.Horizontal,
                    FixedPanel = SplitterFixedPanel.Panel2,

                    Panel1 = new TreeGridView
                    {
                        DataStore = folders
                    },
                    Panel2 = new FixedSplitter((int)ToolStripItem.Height + 6)
                    {
                        Orientation = Orientation.Vertical,
                        FixedPanel = SplitterFixedPanel.Panel1,

                        Panel1 = new ToolStrip(),
                        Panel2 = new GridView
                        {
                            ShowHeader = true,
                            BackgroundColor = KnownColors.White,
                            Columns =
                            {
                                new GridColumn
                                {
                                    HeaderText = "Name",
                                    Sortable = true,
                                    AutoSize = true
                                },
                                new GridColumn
                                {
                                    HeaderText = "Size",
                                    Sortable = true,
                                    AutoSize = true
                                }
                            },

                            DataStore = files
                        }
                    }
                }
            };
        }
    }
}
