using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IArchiveForm : IKuriimuForm
    {
        Func<OpenFileEventArgs, Task<bool>> OpenFilesDelegate { get; set; }
    }

    public class OpenFileEventArgs : EventArgs
    {
        public ArchiveFileInfo Afi { get; }
        public IStateInfo StateInfo { get; }
        public Guid PluginId { get; }

        public OpenFileEventArgs(IStateInfo archiveState, ArchiveFileInfo afi, Guid pluginId)
        {
            Afi = afi;
            StateInfo = archiveState;
            PluginId = pluginId;
        }
    }
}
