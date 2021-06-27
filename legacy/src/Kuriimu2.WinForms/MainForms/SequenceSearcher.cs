﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Kore.Utilities.Text;
using Kuriimu2.WinForms.MainForms.Models;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class SequenceSearcher : Form
    {
        private const int SearchLimit_ = 10 * 1024 * 1024;

        private TextSequenceSearcher _textSequenceSearcher;

        public SequenceSearcher()
        {
            InitializeComponent();

            FillEncodings();
        }

        private void FillEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var items = new List<ListItem>
            {
                new ListItem(Encoding.ASCII.EncodingName.Replace("US-",""), Encoding.ASCII),
                new ListItem(Encoding.GetEncoding("SJIS").EncodingName, Encoding.GetEncoding("SJIS")),
                new ListItem(Encoding.Unicode.EncodingName, Encoding.Unicode),
                new ListItem(Encoding.BigEndianUnicode.EncodingName, Encoding.BigEndianUnicode),
                new ListItem(Encoding.UTF7.EncodingName, Encoding.UTF7),
                new ListItem(Encoding.UTF8.EncodingName, Encoding.UTF8),
            };
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

        private void textSequenceSearcher_FoundMatch(object sender, FoundMatchEventArgs e)
        {
            InvokeAction(() => lstResults.Items.Add(e.Result));
        }

        private void InvokeAction(Action controlAction)
        {
            if (InvokeRequired)
                Invoke(controlAction);
            else
                controlAction();
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

            _textSequenceSearcher = new TextSequenceSearcher(fbd.SelectedPath, SearchLimit_);
            _textSequenceSearcher.FoundMatch += textSequenceSearcher_FoundMatch;

            UpdateSequenceSearcher();
        }

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            lstResults.Items.Clear();

            if (txtSearchDirectory.Text.Trim() == string.Empty || txtSearchText.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Choose a directory to search in and a text to search.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ToggleUi(false);
            await _textSequenceSearcher.SearchAsync(txtSearchText.Text.Trim());
            ToggleUi(true);
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _textSequenceSearcher?.Cancel();
        }

        private void ToggleUi(bool toggle)
        {
            btnCancel.Enabled = !toggle;

            btnBrowse.Enabled = btnSearch.Enabled = toggle;
            txtSearchText.Enabled = chkSearchSubfolders.Enabled = cmbEncoding.Enabled = toggle;
        }
    }
}
