using Eto.Forms;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class TextForm : Panel, IKuriimuForm
    {
        private readonly FormInfo _formInfo;

        private bool _toggle = true;

        public TextForm(FormInfo formInfo)
        {
            InitializeComponent();

            _formInfo = formInfo;

            firstEntryText.Text = (_formInfo.FileState.PluginState as ITextState).Texts[0].GetText().Serialize(_toggle);

            toggleCommand.Executed += _toggleCommand_Executed;
            saveCommand.Executed += SaveCommand_Executed;
        }

        private void _toggleCommand_Executed(object sender, System.EventArgs e)
        {
            _toggle = !_toggle;
            firstEntryText.Text = (_formInfo.FileState.PluginState as ITextState).Texts[0].GetText().Serialize(_toggle);
        }

        private async void SaveCommand_Executed(object sender, System.EventArgs e)
        {
            if (!await _formInfo.FormCommunicator.Save(true))
                MessageBox.Show("Save Error", MessageBoxButtons.OK);

            (_formInfo.FileState.PluginState as ITextState).Texts[0].GetText().Serialize(_toggle);
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
    }
}
