using Eto.Forms;
using Eto.Drawing;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Resources;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms
{
    public partial class MainForm : Form
    {
        #region Localization Keys

        private static string FileKey_ = "File";
        private static string ToolsKey_ = "Tools";
        private static string CiphersKey_ = "Ciphers";
        private static string CompressionKey_ = "Compression";
        private static string SettingsKey_ = "Settings";

        private static string OpenKey_ = "Open";
        private static string OpenWithKey_ = "OpenWith";
        private static string SaveAllKey_ = "SaveAll";

        private static string TextSequenceSearcherKey_ = "TextSequenceSearcher";
        private static string BatchExtractorKey_ = "BatchExtractor";
        private static string BatchInjectorKey_ = "BatchInjector";

        private static string HashesKey_ = "Hashes";
       
        private static string EncryptKey_ = "Encrypt";
        private static string DecryptKey_ = "Decrypt";

        private static string DecompressKey_ = "Decompress";
        private static string CompressKey_ = "Compress";

        private static string RawImageViewerKey_ = "RawImageViewer";

        private static string IncludeDeveloperBuildsKey_ = "IncludeDevBuilds";

        private static string AboutKuriimuKey_ = "AboutKuriimu2";

        #endregion

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

            openFileCommand = new Command { MenuText = Localize(OpenKey_), Shortcut = OpenHotKey, Image = ImageResources.Actions.Open };
            // TODO: Separate open *with* icon?
            openFileWithCommand = new Command { MenuText = Localize(OpenWithKey_), Shortcut = OpenWithHotKey, Image = ImageResources.Actions.OpenWith };
            saveAllFileCommand = new Command { MenuText = Localize(SaveAllKey_), Shortcut = SaveAllHotKey, Image = ImageResources.Actions.SaveAll };

            openTextSequenceSearcherCommand = new Command { MenuText = Localize(TextSequenceSearcherKey_), Image = ImageResources.Actions.Text };
            openBatchExtractorCommand = new Command { MenuText = Localize(BatchExtractorKey_), Image = ImageResources.Actions.BatchExtract };
            openBatchInjectorCommand = new Command { MenuText = Localize(BatchInjectorKey_), Image = ImageResources.Actions.BatchArchive };

            openHashcommand = new Command { MenuText = Localize(HashesKey_), Image = ImageResources.Actions.Hashes };

            openEncryptionCommand = new Command { MenuText = Localize(EncryptKey_) };
            openDecryptionCommand = new Command { MenuText = Localize(DecryptKey_) };

            openDecompressionCommand = new Command { MenuText = Localize(DecompressKey_) };
            openCompressionCommand = new Command { MenuText = Localize(CompressKey_) };

            openRawImageViewerCommand = new Command { MenuText = Localize(RawImageViewerKey_), Image = ImageResources.Actions.ImageViewer };
            //openImageTranscoderCommand = new Command { MenuText = "Image Trascoder" };

            includeDevBuildCommand = new Command();

            openAboutCommand = new Command { MenuText = Localize(AboutKuriimuKey_), Image = ImageResources.Actions.About };

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
                    new ButtonMenuItem { Text = Localize(FileKey_),
                        Items =
                        {
                            openFileCommand, 
                            openFileWithCommand, 
                            new SeparatorMenuItem(), 
                            saveAllFileCommand
                        } 
                    },
                    
                    new ButtonMenuItem { Text = Localize(ToolsKey_),
                        Items =
                        {
                            openBatchExtractorCommand,
                            openBatchInjectorCommand,
                            openTextSequenceSearcherCommand,
                            openHashcommand,
                            openRawImageViewerCommand,
                        }
                    },
                    
                    new ButtonMenuItem { Text = Localize(CiphersKey_),
                        Items =
                        {
                            openDecryptionCommand,
                            openEncryptionCommand 
                        }
                    },
                    
                    new ButtonMenuItem { Text = Localize(CompressionKey_),
                        Items =
                        {
                            openDecompressionCommand, 
                            openCompressionCommand
                        }
                    },
                    
                    //new ButtonMenuItem(openImageTranscoderCommand),
                    
                    new ButtonMenuItem { Text = Localize(SettingsKey_),
                        Items =
                        {
                            new CheckMenuItem
                            {
                                Text = Localize(IncludeDeveloperBuildsKey_), 
                                Checked = Settings.Default.IncludeDevBuilds, 
                                Command = includeDevBuildCommand
                            }
                        } 
                    }
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
