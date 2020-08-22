using Kontract;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Archive;

namespace Kore.Models.LoadInfo
{
    class VirtualLoadInfo
    {
        public IStateInfo ParentStateInfo { get; }

        public IArchiveState ArchiveState { get; }

        public ArchiveFileInfo Afi { get; }

        public IFilePlugin Plugin { get; }

        public VirtualLoadInfo(IStateInfo parentStateInfo, IArchiveState archiveState, ArchiveFileInfo afi)
        {
            ContractAssertions.IsNotNull(parentStateInfo, nameof(parentStateInfo));
            ContractAssertions.IsNotNull(archiveState, nameof(archiveState));
            ContractAssertions.IsNotNull(afi, nameof(afi));
            ContractAssertions.IsElementContained(archiveState.Files, afi, nameof(archiveState), nameof(afi));

            ParentStateInfo = parentStateInfo;
            ArchiveState = archiveState;
            Afi = afi;
        }

        public VirtualLoadInfo(IStateInfo parentStateInfo, IArchiveState archiveState, ArchiveFileInfo afi, IFilePlugin plugin) :
            this(parentStateInfo, archiveState, afi)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));

            Plugin = plugin;
        }
    }
}
