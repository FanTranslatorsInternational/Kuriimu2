using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace Kuriimu2.WinForms.MainForms.Interfaces
{
    public interface IArchiveForm : IKuriimuForm
    {
        Func<OpenFileEventArgs, Task<bool>> OpenFilesDelegate { get; set; }

        Action<RenameFileEventArgs> RenameFilesDelegate { get; set; }

        Func<DeleteFileEventArgs, Task<bool>> DeleteFilesDelegate { get; set; }
    }

    public class OpenFileEventArgs : EventArgs
    {
        public IStateInfo StateInfo { get; }
        public ArchiveFileInfo Afi { get; }
        public Guid PluginId { get; }

        public OpenFileEventArgs(IStateInfo archiveState, ArchiveFileInfo afi, Guid pluginId)
        {
            Afi = afi;
            StateInfo = archiveState;
            PluginId = pluginId;
        }
    }

    public class RenameFileEventArgs : EventArgs
    {
        public IStateInfo StateInfo { get; }
        public ArchiveFileInfo Afi { get; }
        public UPath RenamePath { get; }

        public RenameFileEventArgs(IStateInfo stateInfo, ArchiveFileInfo afi, UPath renamePath)
        {
            StateInfo = stateInfo;
            Afi = afi;
            RenamePath = renamePath;
        }
    }

    public class DeleteFileEventArgs : EventArgs
    {
        public IStateInfo StateInfo { get; }
        public ArchiveFileInfo Afi { get; }

        public DeleteFileEventArgs(IStateInfo stateInfo, ArchiveFileInfo afi)
        {
            StateInfo = stateInfo;
            Afi = afi;
        }
    }
}
