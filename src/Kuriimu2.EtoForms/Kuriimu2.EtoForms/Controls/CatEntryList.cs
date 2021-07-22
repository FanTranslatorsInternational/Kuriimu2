using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Text;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Controls
{
    class CatEntryList : Scrollable
    {
        private readonly TableLayout _layout;
        private readonly ITextState _textState;

        private readonly IList<(TextEntry, ProcessedText, Label)> _sourceTexts;
        private readonly IList<(TextEntry, ProcessedText, Label)> _targetTexts;
        private readonly IList<(Label, Label)> _labelPairs;

        private int _selectedEntryIndex = -1;

        public event EventHandler<EntryChangedEventArgs> EntryChanged;

        #region Properties

        public int Count => _sourceTexts.Count;

        public int SelectedIndex => _selectedEntryIndex;

        #endregion

        public CatEntryList(ITextState textTextState)
        {
            _layout = new TableLayout();
            _textState = textTextState;

            _sourceTexts = new List<(TextEntry, ProcessedText, Label)>();
            _targetTexts = new List<(TextEntry, ProcessedText, Label)>();
            _labelPairs = new List<(Label, Label)>();

            Content = _layout;
            LoadTextEntries();
        }

        public TextEntry GetEntry(int index)
        {
            // TODO: Clone TextEntry, so only a copy gets edited at all times externally (if even)?
            return _sourceTexts[index].Item1;
        }

        public ProcessedText GetText(int index)
        {
            // TODO: Clone processed text, so only a copy gets edited at all times externally?
            return _targetTexts[index].Item2;
        }

        public void SetText(int index, ProcessedText processedText)
        {
            _targetTexts[index] = (_targetTexts[index].Item1, processedText, _targetTexts[index].Item3);

            // Update UI with new text
            _targetTexts[index].Item3.Text = processedText.Serialize();
        }

        #region Load entries

        private void LoadTextEntries()
        {
            // Clear existing entries
            _layout.Rows.Clear();
            _labelPairs.Clear();
            _sourceTexts.Clear();

            // Add new entries
            var controlWidth = GetControlWidth();

            var labelIndex = 0;
            for (var i = 0; i < _textState.Texts.Count; i++)
            {
                var entry = _textState.Texts[i];
                var name = string.IsNullOrEmpty(entry.Name) ? $"Entry {i}" : entry.Name;

                // Add entry header
                _layout.Rows.Add(new Label { Text = name });

                // Add paged texts
                var pagedText = entry.GetPagedText();
                foreach (var page in pagedText)
                {
                    var serializedText = page.Serialize();
                    var sourceLabel = new Label { Text = serializedText, Tag = labelIndex, BackgroundColor = GetBackgroundColor(labelIndex), Size = new Size(controlWidth / 2, 19) };
                    var targetLabel = new Label { Text = serializedText, Tag = labelIndex, BackgroundColor = GetBackgroundColor(labelIndex), Size = new Size(controlWidth / 2, 19) };

                    _sourceTexts.Add((entry, page, sourceLabel));
                    _targetTexts.Add((entry, page, targetLabel));
                    _labelPairs.Add((sourceLabel, targetLabel));

                    sourceLabel.MouseUp += TextLabel_MouseUp;
                    targetLabel.MouseUp += TextLabel_MouseUp;

                    AddTextEntry(_layout, sourceLabel, targetLabel);

                    labelIndex++;
                }
            }
        }

        private void AddTextEntry(TableLayout layout, Label sourceLabel, Label targetLabel)
        {
            layout.Rows.Add(new TableRow
            {
                Cells =
                {
                    new TableLayout
                    {
                        Spacing = new Size(3,1),
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    new TableCell(sourceLabel){ScaleWidth = true},
                                    new TableCell(targetLabel){ScaleWidth = true}
                                }
                            }
                        }
                    }
                }
            });
        }

        #endregion

        #region Events

        private void TextLabel_MouseUp(object sender, MouseEventArgs e)
        {
            // Reset background color on previous entry
            if (_selectedEntryIndex >= 0)
            {
                _labelPairs[_selectedEntryIndex].Item1.BackgroundColor = GetBackgroundColor(_selectedEntryIndex);
                _labelPairs[_selectedEntryIndex].Item2.BackgroundColor = GetBackgroundColor(_selectedEntryIndex);
            }

            // Set background color for new selected entry
            var newEntryIndex = (int)(sender as Label).Tag;

            _labelPairs[newEntryIndex].Item1.BackgroundColor = KnownColors.SelectedLabelColor;
            _labelPairs[newEntryIndex].Item2.BackgroundColor = KnownColors.SelectedLabelColor;

            _selectedEntryIndex = newEntryIndex;

            // Invoke entry changed event
            OnEntryChanged();
        }

        #endregion

        #region Event Handlers

        protected virtual void OnEntryChanged()
        {
            EntryChanged?.Invoke(this, new EntryChangedEventArgs(_sourceTexts[_selectedEntryIndex].Item2, _targetTexts[_selectedEntryIndex].Item2, GetEntry(_selectedEntryIndex).CanParseControlCodes));
        }

        #endregion

        #region Support

        private Color GetBackgroundColor(int index)
        {
            return index % 2 == 0 ? KnownColors.LabelColor1 : KnownColors.LabelColor2;
        }

        protected virtual int GetControlWidth()
        {
            // HACK: This depends on an assumption around the Kuriimu2 design of the TextForm
            // HACK: If the Kuriimu2 TextForm design changes, this has to be changed manually to fit the design changes.

            // If control is automatically sized, assume half the MainForm.Size.Width
            if (Size.Width < 0)
                return Application.Instance.MainForm.Size.Width / 2 - 40;

            return Size.Width / 2;
        }

        #endregion
    }

    class EntryChangedEventArgs : EventArgs
    {
        public ProcessedText SourceText { get; }

        public ProcessedText TargetText { get; }

        public bool CanParseControlCodes { get; }

        public EntryChangedEventArgs(ProcessedText source, ProcessedText target, bool canParseControlCodes)
        {
            SourceText = source;
            TargetText = target;
            CanParseControlCodes = canParseControlCodes;
        }
    }
}
