using System;
using Eto.Forms;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Text;
using Kuriimu2.EtoForms.Controls;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel, IKuriimuForm
    {
        private readonly FormInfo<ITextState> _formInfo;

        public TextForm(FormInfo<ITextState> formInfo)
        {
            _formInfo = formInfo;

            InitializeComponent();

            entryList.EntryChanged += EntryList_EntryChanged;
            targetText.TextChanged += TargetText_TextChanged;
        }

        #region Forminterface methods

        public bool HasRunningOperations()
        {
            return false;
        }

        public void CancelOperations()
        {
        }

        #endregion

        #region Update

        public void UpdateForm()
        {

        }

        #endregion

        #region Events

        private void EntryList_EntryChanged(object sender, EntryChangedEventArgs e)
        {
            sourceText.Text = e.SourceText.Serialize();
            targetText.Text = e.TargetText.Serialize();

            withoutCodeLabel.Visible = e.CanParseControlCodes;
            withoutCodeLabel.Text = e.TargetText.Serialize(false);
        }

        private void TargetText_TextChanged(object sender, EventArgs e)
        {
            entryList.SetText(entryList.SelectedIndex, ProcessedText.Parse(targetText.Text));
            withoutCodeLabel.Text = entryList.GetText(entryList.SelectedIndex).Serialize(false);
        }

        #endregion
    }
}
