using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Text;
using Kore;
using Kuriimu2_WinForms.Interfaces;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class TextForm : UserControl, IKuriimuForm
    {
        private ITextAdapter _textAdapter => Kfi.Adapter as ITextAdapter;
        private List<TextEntry> _textEntries;
        private TabPage _currentTab;
        private TabPage _parentTab;
        private IArchiveAdapter _parentAdapter;
        private IList<IGameAdapter> _gameAdapters;

        private int _selectedPreviewPluginIndex;
        private int _selectedTextEntryIndex;
        private IGameAdapter _selectedGameAdapter => _gameAdapters[_selectedPreviewPluginIndex];

        public TextForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage, IList<IGameAdapter> gameAdapters)
        {
            InitializeComponent();

            Kfi = kfi;

            _currentTab = tabPage;
            _parentTab = parentTabPage;
            _parentAdapter = parentAdapter;
            _gameAdapters = gameAdapters;
            _textEntries = _textAdapter.Entries.ToList();

            LoadGameAdapters();
            LoadEntries();

            UpdatePreview();
            UpdateForm();
        }

        public KoreFileInfo Kfi { get; set; }

        public Color TabColor { get; set; }

        public event EventHandler<OpenTabEventArgs> OpenTab;
        public event EventHandler<SaveTabEventArgs> SaveTab;
        public event EventHandler<CloseTabEventArgs> CloseTab;
        public event EventHandler<ProgressReport> ReportProgress;

        public void Close()
        {
            throw new NotImplementedException();
        }

        #region Utilities

        #region Save
        public void Save(string filename = "")
        {
            SaveTab?.Invoke(this, new SaveTabEventArgs(Kfi) { NewSaveFile = filename });

            UpdateParent();
            UpdateForm();
        }

        private void SaveAs()
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(Kfi.StreamFileInfo.FileName);
            sfd.Filter = "All Files (*.*)|*.*";

            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
            else
                MessageBox.Show("No save location was chosen.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Load
        private void LoadGameAdapters()
        {
            tlsPreviewPlugin.DropDownItems.AddRange(_gameAdapters.Select(g =>
                new ToolStripMenuItem
                {
                    Text = g.GetType().GetCustomAttributes(typeof(PluginInfoAttribute), false).Cast<PluginInfoAttribute>().FirstOrDefault()?.Name,
                    Image = string.IsNullOrEmpty(g.IconPath) || !File.Exists(g.IconPath) ? null : new Bitmap(g.IconPath)
                }).ToArray());

            if (tlsPreviewPlugin.DropDownItems.Count > 0)
                foreach (var tsb in tlsPreviewPlugin.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += PreviewItem_Click;

            _selectedPreviewPluginIndex = 0;
        }

        private void PreviewItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LoadEntries()
        {
            lstText.Items.Clear();
            lstText.Items.AddRange(_textAdapter.Entries.Select(x => new ListViewItem(new[] { x.Name, x.OriginalText, x.OriginalText, x.Notes }) { Tag = x }).ToArray());
        }
        #endregion

        #region Updates
        public void UpdateForm()
        {
            _currentTab.Text = Kfi.DisplayName;

            // Menu
            tlsMainSave.Enabled = _textAdapter is ISaveFiles;
            tlsMainSaveAs.Enabled = _textAdapter is ISaveFiles && Kfi.ParentKfi == null;
            //tsbProperties.Enabled = _archiveAdapter.FileHasExtendedProperties;

            // Text
            tlsTextAdd.Enabled = _textAdapter is ITextAddEntries;
            tlsTextEntryCount.Text = $"Entries: {_textAdapter.Entries.Count()}";

            // Preview
            tlsPreviewPlugin.Enabled = _gameAdapters.Any();
            tlsPreviewPlugin.Text = tlsPreviewPlugin.Enabled ? tlsPreviewPlugin.DropDownItems[_selectedPreviewPluginIndex].Text : "No game plugin";
            tlsPreviewPlugin.Image = tlsPreviewPlugin.Enabled ? tlsPreviewPlugin.DropDownItems[_selectedPreviewPluginIndex].Image : null;
        }

        public void UpdateParent()
        {
            if (_parentTab != null)
                if (_parentTab.Controls[0] is IArchiveForm archiveForm)
                {
                    archiveForm.UpdateForm();
                    archiveForm.UpdateParent();
                }
        }

        private void UpdatePreview()
        {
            if (!_textEntries.Any() || _selectedTextEntryIndex < 0)
            {
                imgPreview.Image = null;
                return;
            }

            if (_selectedGameAdapter is IGenerateGamePreviews generator)
                imgPreview.Image = generator.GeneratePreview(_textEntries[_selectedTextEntryIndex]);
        }
        #endregion

        #endregion

        #region Events
        private void tlsMainSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void tlsMainSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs();
        }
        #endregion

        private void lstText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstText.SelectedIndices.Count > 0)
                _selectedTextEntryIndex = lstText.SelectedIndices[0];
        }
    }
}
