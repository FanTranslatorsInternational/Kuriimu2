using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Kore.Utilities;
using Kuriimu2.WinForms.MainForms.Models;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class SequenceSearcher : Form
    {
        private const int SearchLimit = 10 * 1024 * 1024;

        private TextSequenceSearcher _textSequenceSearcher;

        public SequenceSearcher()
        {
            InitializeComponent();

            FillEncodings();
        }

        private void FillEncodings()
        {
            List<ListItem> items = new List<ListItem>();
            foreach (EncodingInfo enc in Encoding.GetEncodings())
            {
                string name = enc.GetEncoding().EncodingName;
                if (name.Contains("ASCII") || name.Contains("Shift-JIS") || (name.Contains("Unicode") && !name.Contains("32")))
                    items.Add(new ListItem(name.Replace("US-", ""), enc.GetEncoding()));
            }
            items.Sort();

            cmbEncoding.DisplayMember = "Text";
            cmbEncoding.ValueMember = "Value";
            cmbEncoding.DataSource = items;
            cmbEncoding.SelectedValue = Encoding.ASCII;
        }

        private void UpdateSequenceSearcher()
        {
            if (_textSequenceSearcher == null)
                return;

            _textSequenceSearcher.Encoding = (Encoding)cmbEncoding.SelectedValue;
            _textSequenceSearcher.IsSearchSubDirectories = chkSearchSubfolders.Checked;
        }

        private void ChkSearchSubfolders_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSequenceSearcher();
        }

        private void CmbEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSequenceSearcher();
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog
            {
                Description = "Select the directory to search through.",
                SelectedPath = Settings.Default.SequenceSearchDirectory
            };

            if (fbd.ShowDialog() != DialogResult.OK)
                return;

            txtSearchDirectory.Text = fbd.SelectedPath;

            Settings.Default.SequenceSearchDirectory = fbd.SelectedPath;
            Settings.Default.Save();

            _textSequenceSearcher = new TextSequenceSearcher(fbd.SelectedPath, SearchLimit);
            UpdateSequenceSearcher();
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            lstResults.Items.Clear();

            if (txtSearchDirectory.Text.Trim() == string.Empty || txtSearchText.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Choose a directory to search in and a text to search.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = _textSequenceSearcher.Search(txtSearchText.Text.Trim());
            lstResults.Items.AddRange(results.ToArray());
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
