using System;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IKuriimuForm
    {
        Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        Action<IStateInfo> UpdateTabDelegate { get; set; }

        void UpdateForm();
    }

    public class SaveTabEventArgs : EventArgs
    {
        public IStateInfo StateInfo { get; }

        public UPath SavePath { get; }

        public SaveTabEventArgs(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            StateInfo = stateInfo;
        }

        public SaveTabEventArgs(IStateInfo stateInfo, UPath savePath) :
            this(stateInfo)
        {
            SavePath = savePath;
        }
    }
}
