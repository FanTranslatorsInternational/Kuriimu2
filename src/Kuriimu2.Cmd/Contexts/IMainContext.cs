using System;
using System.Threading.Tasks;
using Kontract.Interfaces.Managers;
using Kontract.Models.Archive;

namespace Kuriimu2.Cmd.Contexts
{
    interface IMainContext : IContext
    {
        Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId);

        Task SaveFile(IStateInfo stateInfo);

        void CloseFile(IStateInfo stateInfo);
    }
}
