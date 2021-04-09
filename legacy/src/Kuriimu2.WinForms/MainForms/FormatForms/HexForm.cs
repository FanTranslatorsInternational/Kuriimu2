using System;
using System.Windows.Forms;
using Be.Windows.Forms;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kuriimu2.WinForms.MainForms.Interfaces;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    public partial class HexForm : UserControl, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly IHexState _hexState;
        private readonly IFormCommunicator _formCommunicator;

        public HexForm(IStateInfo stateInfo, IFormCommunicator formCommunicator)
        {
            InitializeComponent();

            if (!(stateInfo.PluginState is IHexState hexState))
                throw new InvalidOperationException($"This state is not an {nameof(IHexState)}.");

            _stateInfo = stateInfo;
            _hexState = hexState;
            _formCommunicator = formCommunicator;

            fileData.ByteProvider = new DynamicFileByteProvider(hexState.FileStream);
        }

        public void UpdateForm()
        {
            _formCommunicator.Update(true, false);
        }
    }
}