using System;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class HexForm : IKuriimuForm
    {
        public HexForm(FormInfo<IHexState> formInfo)
        {
            InitializeComponent();

            hexBox.LoadStream(formInfo.PluginState?.FileStream);
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
            throw new NotImplementedException();
        }

        #endregion
    }
}
