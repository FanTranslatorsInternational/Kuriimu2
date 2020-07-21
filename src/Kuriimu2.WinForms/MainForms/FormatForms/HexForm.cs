using System;
using System.Threading.Tasks;
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

        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        public HexForm(IStateInfo stateInfo)
        {
            InitializeComponent();

            if (!(stateInfo.PluginState is IHexState hexState))
                throw new InvalidOperationException($"This state is not an {nameof(IHexState)}.");

            _stateInfo = stateInfo;
            _hexState = hexState;

            fileData.ByteProvider = new DynamicFileByteProvider(hexState.FileStream);
        }

        public void UpdateForm()
        {
            UpdateTabDelegate(_stateInfo);
        }
    }
}