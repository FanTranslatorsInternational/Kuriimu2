using Eto.Forms;
using Eto.Drawing;
using Kuriimu2.EtoForms.Controls;

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

            openFileCommand = new Command { MenuText = "Open", Shortcut=OpenHotKey };
            openFileWithCommand = new Command { MenuText = "Open with Plugin", Shortcut=OpenWithHotKey };
            saveAllFileCommand = new Command { MenuText = "Save All", Shortcut = SaveAllHotKey, Image= MenuSaveResource };

            openTextSequenceSearcherCommand = new Command { MenuText = "Text Sequence Searcher" };
            openBatchExtractorCommand = new Command { MenuText = "Batch Extractor" };
            openBatchInjectorCommand = new Command { MenuText = "Batch Injector" };

            openHashcommand = new Command { MenuText = "Hashes" };

            openEncryptionCommand = new Command { MenuText = "Encrypt" };
            openDecryptionCommand = new Command { MenuText = "Decrypt" };

            openDecompressionCommand = new Command { MenuText = "Decompress" };
            openCompressionCommand = new Command { MenuText = "Compress" };

            openRawImageViewerCommand = new Command { MenuText = "Raw Image Viewer" };

            openImageTranscoderCommand = new Command { MenuText = "Image Trascoder" };

            openAboutCommand = new Command { MenuText = "About..." };

            #endregion

            Title = "Kuriimu2";
            ClientSize = new Size(1116, 643);
            Padding = new Padding(3);
            Icon = Icon.FromResource("Kuriimu2.EtoForms.Images.kuriimu2winforms.ico");

            #region Menu

            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem { Text = "File", Items = { openFileCommand, openFileWithCommand, new SeparatorMenuItem(), saveAllFileCommand } },
                    new ButtonMenuItem { Text = "Tools", Items = { openBatchExtractorCommand, openBatchInjectorCommand, openTextSequenceSearcherCommand } },
                    new ButtonMenuItem(openHashcommand),
                    new ButtonMenuItem { Text = "Ciphers", Items = { openEncryptionCommand, openDecryptionCommand } },
                    new ButtonMenuItem { Text = "Compressions", Items = { openDecompressionCommand, openCompressionCommand } },
                    new ButtonMenuItem(openRawImageViewerCommand),
                    //new ButtonMenuItem(openImageTranscoderCommand)
                },
                AboutItem = openAboutCommand
            };

            #endregion

            #region Content

            tabControl = new TabControl();
            _progressBarEx = new ProgressBarEx();
            statusMessage = new Label();

            Content = new Splitter
            {
                AllowDrop = true,

                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel2,

                Position = 616,
                Panel2MinimumSize = 24,

                Panel1 = tabControl,
                Panel2 = new FixedSplitter(450)
                {
                    Orientation = Orientation.Horizontal,

                    Panel1 = _progressBarEx,
                    Panel2 = statusMessage
                }
            };

            #endregion
        }
    }
}
