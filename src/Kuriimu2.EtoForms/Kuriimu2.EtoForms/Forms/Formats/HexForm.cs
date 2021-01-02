using System;
using System.Collections.Generic;
using System.Text;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.EtoForms.Forms.Interfaces;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class HexForm : IKuriimuForm
    {
        public HexForm(IStateInfo stateInfo, IFormCommunicator communicator)
        {
            InitializeComponent();

            hexBox.LoadStream((stateInfo.PluginState as IHexState)?.FileStream);
        }

        #region Update

        public void UpdateForm()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
