using Eto.Forms;
using Eto.Drawing;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Resources;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms
{
    public partial class MainForm : Form
    {
        #region Commands

        private Command openFileCommand;
        private Command openFileWithCommand;
        private Command saveAllFileCommand;

        private Command openTextSequenceSearcherCommand;
        private Command openBatchExtractorCommand;
        private Command openBatchInjectorCommand;

        private Command openHashcommand;

        private Command openEncryptionCommand;
        private Command openDecryptionCommand;

        private Command openDecompressionCommand;
        private Command openCompressionCommand;

        private Command openRawImageViewerCommand;

        private Command openImageTranscoderCommand;

        private Command includeDevBuildCommand;

        private Command openAboutCommand;

        #endregion

        #region Controls

        private TabControl tabControl;
        private ProgressBarEx _progressBarEx;
        private Label statusMessage;

        #endregion

        void InitializeComponent()
        {
            #region Commands

            openFileCommand = new Command { MenuText = "Open", Shortcut = OpenHotKey, Image = ImageResources.Actions.Open };
            //TODO separate open *with* icon?
            openFileWithCommand = new Command { MenuText = "Open with Plugin", Shortcut = OpenWithHotKey, Image = ImageResources.Actions.OpenWith };
            saveAllFileCommand = new Command { MenuText = "Save All", Shortcut = SaveAllHotKey, Image = ImageResources.Actions.SaveAll };

            openTextSequenceSearcherCommand = new Command { MenuText = "Text Sequence Searcher", Image = ImageResources.Actions.Text };
            openBatchExtractorCommand = new Command { MenuText = "Batch Extractor", Image = ImageResources.Actions.BatchExtract };
            openBatchInjectorCommand = new Command { MenuText = "Batch Injector", Image = ImageResources.Actions.BatchArchive };

            openHashcommand = new Command { MenuText = "Hashes", Image = ImageResources.Actions.Hashes };

            openEncryptionCommand = new Command { MenuText = "Encrypt" };
            openDecryptionCommand = new Command { MenuText = "Decrypt" };

            openDecompressionCommand = new Command { MenuText = "Decompress" };
            openCompressionCommand = new Command { MenuText = "Compress" };

            openRawImageViewerCommand = new Command { MenuText = "Raw Image Viewer", Image = ImageResources.Actions.ImageViewer };
            //openImageTranscoderCommand = new Command { MenuText = "Image Trascoder" };

            includeDevBuildCommand = new Command();

            openAboutCommand = new Command { MenuText = "About Kuriimu", Image = ImageResources.Actions.About };

            #endregion

            Title = "Kuriimu2";
            ClientSize = new Size(1116, 643);
            Padding = new Padding(3);
            Icon = Icon.FromResource("Kuriimu2.EtoForms.Images.Misc.kuriimu2.ico");

            #region Menu

            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem { Text = "File",
                        Image = ImageResources.Menu.File,
                        Items =
                        {
                            openFileCommand, 
                            openFileWithCommand, 
                            new SeparatorMenuItem(), 
                            saveAllFileCommand
                        } 
                    },
                    
                    new ButtonMenuItem { Text = "Tools",
                        Image = ImageResources.Menu.Tools,
                        Items =
                        {
                            openBatchExtractorCommand,
                            openBatchInjectorCommand,
                            openTextSequenceSearcherCommand,
                            openHashcommand,
                            openRawImageViewerCommand,
                        }
                    },
                    
                    new ButtonMenuItem { Text = "Ciphers",
                        Image = ImageResources.Menu.Ciphers,
                        Items =
                        {
                            openDecryptionCommand,
                            openEncryptionCommand 
                        }
                    },
                    
                    new ButtonMenuItem { Text = "Compression",
                        Image = ImageResources.Menu.Compression,
                        Items =
                        {
                            openDecompressionCommand, 
                            openCompressionCommand
                        }
                    },
                    
                    //new ButtonMenuItem(openImageTranscoderCommand),
                    
                    new ButtonMenuItem { Text = "Settings",
                        Image = ImageResources.Menu.Settings,
                        Items =
                        {
                            new CheckMenuItem
                            {
                                Text = "Include Developer Builds", 
                                Checked = Settings.Default.IncludeDevBuilds, 
                                Command = includeDevBuildCommand
                            }
                        } 
                    }
                },
                
                HelpMenu =
                {
                    Image = ImageResources.Menu.Help
                },
                
                AboutItem = openAboutCommand
            };

            #endregion

            #region Content

            tabControl = new TabControl();
            _progressBarEx = new ProgressBarEx();
            statusMessage = new Label();

            var progressLayout = new TableLayout
            {
                Spacing = new Size(3, 3),

                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(_progressBarEx) { ScaleWidth = true },
                            new TableCell(statusMessage) { ScaleWidth = true },
                        }
                    }
                }
            };

            Content = new TableLayout
            {
                AllowDrop = true,
                Spacing = new Size(3, 3),

                Rows =
                {
                    new TableRow(tabControl) { ScaleHeight = true },
                    progressLayout
                }
            };

            #endregion
        }
    }
}
