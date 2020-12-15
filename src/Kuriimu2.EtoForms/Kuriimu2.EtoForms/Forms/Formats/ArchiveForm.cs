using System;
using Eto.Forms;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kore.Managers.Plugins;
using Kuriimu2.EtoForms.Forms.Interfaces;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    public partial class ArchiveForm : Panel, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly IArchiveFormCommunicator _communicator;
        private readonly PluginManager _pluginManager;
        private readonly IProgressContext _progress;

        public ArchiveForm(IStateInfo stateInfo, IArchiveFormCommunicator communicator, PluginManager pluginManager, IProgressContext progress)
        {
            InitializeComponent();

            _stateInfo = stateInfo;
            _communicator = communicator;
            _pluginManager = pluginManager;
            _progress = progress;
        }

        #region Update

        public void UpdateForm()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
