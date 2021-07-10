using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Text;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Support;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel, IKuriimuForm
    {
        private readonly FormInfo<ITextState> _formInfo;

        public TextForm(FormInfo<ITextState> formInfo)
        {
            _formInfo = formInfo;

            InitializeComponent();
            LoadTextEntries();
        }

        #region Forminterface methods

        public bool HasRunningOperations()
        {
            return false;
        }

        #endregion

        #region Update

        public void UpdateForm()
        {

        }

        #endregion

        #region Load

        // TODO: Remember which rows/cells contain which entry and/or page
        private void LoadTextEntries()
        {
            // Clear existing entries
            entryLayout.Items.Clear();

            for (var i = 0; i < _formInfo.PluginState.Texts.Count; i++)
            {
                var entry = _formInfo.PluginState.Texts[i];
                var name = string.IsNullOrEmpty(entry.Name) ? $"Entry {i}" : entry.Name;

                // Add entry header
                entryLayout.Items.Add(new StackLayoutItem(new Label { Text = name }));

                // Add paged texts
                var pagedText = entry.GetPagedText();
                foreach (var page in pagedText)
                {
                    entryLayout.Items.Add(new StackLayoutItem(new TableLayout(new TableRow(new TableCell(new Label{Text = page.Serialize(),TextColor = KnownColors.DarkRed}){ScaleWidth = true},new TableCell(new Label{Text = page.Serialize()}){ScaleWidth = true}))));
                }
            }
        }

        #endregion
    }
}
