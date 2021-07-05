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

            _firstEntryText.Text = (_formInfo.FileState.PluginState as ITextState).Texts[3].GetTexts()[0].Serialize(_toggle);

            _toggleCommand.Executed += _toggleCommand_Executed;
        }

        private void _toggleCommand_Executed(object sender, System.EventArgs e)
        {
            _toggle = !_toggle;
            _firstEntryText.Text = (_formInfo.FileState.PluginState as ITextState).Texts[3].GetTexts()[0].Serialize(_toggle);
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
