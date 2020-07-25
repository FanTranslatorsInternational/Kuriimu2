using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Interfaces.Progress;
using Kontract.Models.IO;
using Kontract.Models.Text;
using Kuriimu2.WinForms.MainForms.Interfaces;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    public partial class TextForm : UserControl, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly ITextState _textState;
        private readonly IProgressContext _progressContext;
        private readonly IFormCommunicator _formCommunicator;

        private readonly IList<TextEntry> _textEntries;
        private readonly IList<IGameAdapter> _gameAdapters;

        private IGameAdapter SelectedGameAdapter => _gameAdapters.Any() ? _gameAdapters[_selectedPreviewPluginIndex] : null;

        private int _selectedPreviewPluginIndex;
        private int _selectedTextEntryIndex;

        public TextForm(IStateInfo state,IFormCommunicator formCommunicator, IList<IGameAdapter> gameAdapters, IProgressContext progressContext)
        {
            InitializeComponent();

            if (!(state.PluginState is ITextState textState))
                throw new InvalidOperationException($"The state is no '{nameof(ITextState)}'.");

            _stateInfo = state;
            _textState = textState;
            _progressContext = progressContext;
            _formCommunicator = formCommunicator;

            _textEntries = textState.Texts;
            _gameAdapters = gameAdapters;

            LoadGameAdapters();
            LoadEntries();

            UpdatePreview();
            UpdateForm();
        }

        private void LoadGameAdapters()
        {
            var items = new List<ToolStripItem>(_gameAdapters.Count);
            foreach (var gameAdapter in _gameAdapters)
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

        private void LoadEntries()
        {
            lstText.Items.Clear();

            var items = new List<ListViewItem>(_textEntries.Count);
            foreach (var entry in _textEntries)
            {
                var itemContent = new[]
                {
                    entry.Name,
                    entry.OriginalText,
                    entry.EditedText,
                    entry.Notes
                };
                items.Add(new ListViewItem(itemContent)
                {
                    Tag = entry
                });
            }

            lstText.Items.AddRange(items.ToArray());
        }

        #region Save

        private async void Save(bool saveAs)
        {
            var wasSaved = await _formCommunicator.Save(saveAs);

            if (wasSaved)
                _formCommunicator.ReportStatus(true, "File saved successfully.");
            else
                _formCommunicator.ReportStatus(false, "File not saved successfully.");

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        #endregion

        #region Updates

        public void UpdateForm()
        {
            UpdateProperties();
        }

        private void UpdateProperties()
        {
            // Menu
            tlsMainSave.Enabled = _textState is ISaveFiles;
            tlsMainSaveAs.Enabled = _textState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            // Text
            tlsTextAdd.Enabled = _textState is IAddEntries;
            tlsTextEntryCount.Text = $"Entries: {_textEntries.Count}";

            // Preview
            tlsPreviewPlugin.Enabled = _gameAdapters.Any();
            tlsPreviewPlugin.Text = tlsPreviewPlugin.Enabled ? tlsPreviewPlugin.DropDownItems[_selectedPreviewPluginIndex].Text : "No game plugin";
            tlsPreviewPlugin.Image = tlsPreviewPlugin.Enabled ? tlsPreviewPlugin.DropDownItems[_selectedPreviewPluginIndex].Image : null;
        }

        private void UpdatePreview()
        {
            if (SelectedGameAdapter == null)
                return;

            if (!_textEntries.Any() || _selectedTextEntryIndex < 0)
            {
                imgPreview.Image = null;
                return;
            }

            if (SelectedGameAdapter is IGenerateGamePreviews generator)
                imgPreview.Image = generator.GeneratePreview(_textEntries[_selectedTextEntryIndex]);
        }

        #endregion

        #region Events

        private void tlsMainSave_Click(object sender, EventArgs e)
        {
            Save(false);
        }

        private void tlsMainSaveAs_Click(object sender, EventArgs e)
        {
            Save(true);
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

        private void imgPreview_Zoomed(object sender, ImageBoxZoomEventArgs e)
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

        #endregion
    }
}
