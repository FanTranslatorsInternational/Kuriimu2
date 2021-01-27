using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Kore.Utilities.Text;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Resources;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class SequenceSearcher : Dialog
    {
        private const int SearchLimit_ = 10 * 1024 * 1024;

        private TextSequenceSearcher _sequenceSearcher;

        public SequenceSearcher()
        {
            InitializeComponent();

            encodings.DataStore = GetEncodings();
            encodings.SelectedIndex = 0;

            #region Commands

            browseCommand.Executed += browseCommand_Executed;
            executeCommand.Executed += executeCommand_Executed;
            cancelCommand.Executed += cancelCommand_Executed;

            #endregion
        }

        #region Initialization

        private IEnumerable<object> GetEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return new List<object>
            {
                new EncodingElement(Encoding.ASCII),
                new EncodingElement(Encoding.GetEncoding("SJIS")),
                new EncodingElement(Encoding.Unicode),
                new EncodingElement(Encoding.BigEndianUnicode),
                new EncodingElement(Encoding.UTF7),
                new EncodingElement(Encoding.UTF8)
            };
        }

        #endregion

        #region Events

        private void browseCommand_Executed(object sender, EventArgs e)
        {
            var fbd = new SelectFolderDialog
            {
                Title = "Select the directory to search through.",
                Directory = Settings.Default.SequenceSearchDirectory
            };

            if (fbd.ShowDialog(this) != DialogResult.Ok)
                return;

            inputText.Text = fbd.Directory;

            Settings.Default.SequenceSearchDirectory = fbd.Directory;
            Settings.Default.Save();
        }

        private async void executeCommand_Executed(object sender, EventArgs e)
        {
            await Execute();
        }

        private void cancelCommand_Executed(object sender, EventArgs e)
        {
            _sequenceSearcher?.Cancel();
        }

        private void sequenceSearcher_FoundMatch(object sender, FoundMatchEventArgs e)
        {
            Application.Instance.Invoke(() => resultList.Items.Add(e.Result.ToString()));
        }

        #endregion

        #region Execution

        private async Task Execute()
        {
            resultList.Items.Clear();

            if (!ValidateInput())
                return;

            ToggleUi(false);

            _sequenceSearcher = new TextSequenceSearcher(inputText.Text, SearchLimit_);
            _sequenceSearcher.Encoding = ((EncodingElement)encodings.SelectedValue).Encoding;
            _sequenceSearcher.IsSearchSubDirectories = searchSubfoldersBox.Checked ?? false;
            _sequenceSearcher.FoundMatch += sequenceSearcher_FoundMatch;

            await _sequenceSearcher.SearchAsync(searchText.Text);

            ToggleUi(true);
        }

        private bool ValidateInput()
        {
            if (encodings.SelectedIndex < 0)
            {
                MessageBox.Show("Select an encoding.", MessageBoxButtons.OK);
                return false;
            }

            if (string.IsNullOrEmpty(inputText.Text))
            {
                MessageBox.Show("Select a directory to search in.", MessageBoxButtons.OK);
                return false;
            }

            if (string.IsNullOrEmpty(searchText.Text))
            {
                MessageBox.Show("Select a text to search for.", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        private void ToggleUi(bool toggle)
        {
            cancelCommand.Enabled = !toggle;
            browseCommand.Enabled = executeCommand.Enabled = toggle;
            searchText.Enabled = searchSubfoldersBox.Enabled = encodings.Enabled = toggle;
        }

        #endregion
    }
}
