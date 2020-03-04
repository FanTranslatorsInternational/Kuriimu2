using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using Kontract.Attributes;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.IO;
using Kuriimu2.WinForms.MainForms.Interfaces;
using MoreLinq.Experimental;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    public partial class TextForm : UserControl, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly ITextState _textState;

        private List<TextEntry> _textEntries;
        private IList<IGameAdapter> _gameAdapters;

        private IGameAdapter _selectedGameAdapter => _gameAdapters.Any() ? _gameAdapters[_selectedPreviewPluginIndex] : null;

        private int _selectedPreviewPluginIndex;
        private int _selectedTextEntryIndex;

        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        public TextForm(IStateInfo state, IList<IGameAdapter> gameAdapters)
        {
            InitializeComponent();

            var textState = state.State as ITextState;
            if (textState == null)
                throw new InvalidOperationException($"The state is no '{nameof(ITextState)}'.");

            _stateInfo = state;
            _textState = textState;

            LoadGameAdapters(gameAdapters);
            LoadEntries(textState.Texts);

            UpdatePreview();
            UpdateForm();
        }

        private void LoadGameAdapters(IList<IGameAdapter> gameAdapters)
        {
            _gameAdapters = gameAdapters.ToList();

            var items = new List<ToolStripItem>(gameAdapters.Count);
            foreach (var gameAdapter in gameAdapters)
            {
                items.Add(new ToolStripMenuItem
                {
                    Text = gameAdapter.MetaData?.Name,
                    Image = string.IsNullOrWhiteSpace(gameAdapter.IconPath) || !File.Exists(gameAdapter.IconPath) ?
                        null :
                    Image.FromFile(gameAdapter.IconPath),
                    Tag = gameAdapter
                });
            }

            tlsPreviewPlugin.DropDownItems.AddRange(items.ToArray());

            if (tlsPreviewPlugin.DropDownItems.Count > 0)
                foreach (var tsb in tlsPreviewPlugin.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += PreviewItem_Click;

            _selectedPreviewPluginIndex = 0;
        }

        #region Save

        private void SaveAs()
        {
            var sfd = new SaveFileDialog
            {
                FileName = _stateInfo.FilePath.GetName(),
                Filter = "All Files (*.*)|*.*"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
            else
                MessageBox.Show("No save location was chosen.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public async void Save(UPath savePath)
        {
            if (savePath == UPath.Empty)
                savePath = _stateInfo.FilePath;

            var result = await SaveFilesDelegate?.Invoke(new SaveTabEventArgs(_stateInfo, savePath));
            if (!result)
                MessageBox.Show("The file could not be saved.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            UpdateForm();
        }

        #endregion

        #region Load

        private void LoadEntries(IEnumerable<TextEntry> textEntries)
        {
            _textEntries = textEntries.ToList();

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
            tlsTextAdd.Enabled = _textAdapter is IAddEntries;
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
            if (_selectedGameAdapter == null)
                return;

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

        private void PreviewItem_Click(object sender, EventArgs e)
        {
            var tsi = (ToolStripMenuItem)sender;
            _selectedPreviewPluginIndex = _gameAdapters.IndexOf((IGameAdapter)tsi.Tag);

            UpdatePreview();
        }

        private void lstText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstText.SelectedIndices.Count > 0)
                _selectedTextEntryIndex = lstText.SelectedIndices[0];

            UpdatePreview();
        }
        #endregion

        private void imgPreview_Zoomed(object sender, Cyotek.Windows.Forms.ImageBoxZoomEventArgs e)
        {
            tlsPreviewZoom.Text = "Zoom: " + imgPreview.Zoom + "%";
        }

        private void imgPreview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                imgPreview.SelectionMode = ImageBoxSelectionMode.None;
                imgPreview.Cursor = Cursors.SizeAll;
                tlsPreviewTool.Text = "Tool: Pan";
            }
        }

        private void imgPreview_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                imgPreview.SelectionMode = ImageBoxSelectionMode.Zoom;
                imgPreview.Cursor = Cursors.Default;
                tlsPreviewTool.Text = "Tool: Zoom";
            }
        }
    }
}
