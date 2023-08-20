using Kontract;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Plugins.State.Archive;

namespace Kore.Models.Managers.Files.Support
{
    class VirtualLoadInfo
    {
        public IFileState ParentFileState { get; }

        public IArchiveState ArchiveState { get; }

        public ArchiveFileInfo Afi { get; }

        public IFilePlugin Plugin { get; }

        public VirtualLoadInfo(IFileState parentFileState, IArchiveState archiveState, ArchiveFileInfo afi)
        {
            ContractAssertions.IsNotNull(parentFileState, nameof(parentFileState));
            ContractAssertions.IsNotNull(archiveState, nameof(archiveState));
            ContractAssertions.IsNotNull(afi, nameof(afi));
            ContractAssertions.IsElementContained(archiveState.Files, afi, nameof(archiveState), nameof(afi));

            ParentFileState = parentFileState;
            ArchiveState = archiveState;
            Afi = afi;
        }

        public VirtualLoadInfo(IFileState parentFileState, IArchiveState archiveState, ArchiveFileInfo afi, IFilePlugin plugin) :
            this(parentFileState, archiveState, afi)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));

            Plugin = plugin;
        }
    }
}
