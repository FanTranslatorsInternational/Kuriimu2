using System;
using System.Diagnostics.Tracing;
using System.Drawing;
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
        Action<ReportStatusEventArgs> ReportStatusDelegate { get; set; }

        void UpdateForm();
    }

    public class SaveTabEventArgs : EventArgs
    {
        public IStateInfo StateInfo { get; }

        public UPath SavePath { get; }

        public SaveTabEventArgs(IStateInfo stateInfo, UPath savePath)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            StateInfo = stateInfo;
            SavePath = savePath;
        }
    }

    public class ReportStatusEventArgs : EventArgs
    {
        public string Status { get; }

        public Color TextColor { get; }

        public ReportStatusEventArgs(string status) : this(status, Color.Black)
        {
        }

        public ReportStatusEventArgs(string status, Color textColor)
        {
            ContractAssertions.IsNotNull(status, nameof(status));

            Status = status;
            TextColor = textColor;
        }
    }
}
