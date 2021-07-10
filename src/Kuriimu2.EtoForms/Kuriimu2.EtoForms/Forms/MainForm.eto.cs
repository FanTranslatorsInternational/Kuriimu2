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

        private const string FileKey_ = "File";
        private const string ToolsKey_ = "Tools";
        private const string CiphersKey_ = "Ciphers";
        private const string CompressionKey_ = "Compression";
        private const string SettingsKey_ = "Settings";

        private const string OpenKey_ = "Open";
        private const string OpenWithKey_ = "OpenWith";
        private const string SaveAllKey_ = "SaveAll";

        private const string TextSequenceSearcherKey_ = "TextSequenceSearcher";
        private const string BatchExtractorKey_ = "BatchExtractor";
        private const string BatchInjectorKey_ = "BatchInjector";

        private const string HashesKey_ = "Hashes";
       
        private const string EncryptKey_ = "Encrypt";
        private const string DecryptKey_ = "Decrypt";

        private const string DecompressKey_ = "Decompress";
        private const string CompressKey_ = "Compress";

        private const string RawImageViewerKey_ = "RawImageViewer";

        private const string IncludeDeveloperBuildsKey_ = "IncludeDevBuilds";
        private const string ChangeLanguageKey_ = "ChangeLanguage";

        private const string AboutKuriimuKey_ = "AboutKuriimu2";

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

        #region Language Commands

        private Command englishCommand;
        private Command germanCommand;
        private Command dutchCommand;
        private Command russianCommand;
        private Command simpleChineseCommand;

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

            #region Language Commands

            englishCommand = new Command { MenuText = "English" };
            germanCommand = new Command { MenuText = "Deutsch" };
            dutchCommand = new Command { MenuText = "Nederlands" };
            russianCommand = new Command { MenuText = "русский" };
            simpleChineseCommand = new Command { MenuText = "简体中文" };

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
                            },
                            new ButtonMenuItem
                            {
                                Text = Localize(ChangeLanguageKey_),
                                Items =
                                {
                                    englishCommand,
                                    germanCommand,
                                    dutchCommand,
                                    russianCommand,
                                    simpleChineseCommand,

                                }
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
