using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models.IO;
using Kore.Factories;

namespace Kore.Providers
{
    /// <summary>
    /// A class to create file systems.
    /// </summary>
    class FileSystemProvider : IFileSystemProvider
    {
        private IStateInfo _stateInfo;

        public void RegisterStateInfo(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            _stateInfo = stateInfo;
        }

        /// <inheritdoc />
        public IFileSystem CreatePhysicalFileSystem(string path)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            return FileSystemFactory.CreatePhysicalFileSystem(path, _stateInfo.StreamManager);
        }

        /// <inheritdoc />
        public IFileSystem CreateAfiFileSystem(IArchiveState archiveState)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");
            ContractAssertions.IsNotNull(archiveState, nameof(archiveState));

            return CreateAfiFileSystem(archiveState, UPath.Root);
        }

        /// <inheritdoc />
        public IFileSystem CreateAfiFileSystem(IArchiveState archiveState, UPath path)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");
            ContractAssertions.IsNotNull(archiveState, nameof(archiveState));

            return FileSystemFactory.CreateAfiFileSystem(archiveState, path, _stateInfo.StreamManager);
        }
    }
}
