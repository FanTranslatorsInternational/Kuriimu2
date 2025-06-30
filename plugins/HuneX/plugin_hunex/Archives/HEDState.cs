using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_hunex.Archives
{
    class HEDState : ILoadFiles, IArchiveFilePluginState
    {
        private readonly HED _hed = new();
        private List<IArchiveFile> _files;

        public IReadOnlyList<IArchiveFile> Files => _files;

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream hedStream = await fileSystem.OpenFileAsync(filePath);
            Stream mrgStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("mrg"));

            Stream namStream = null;
            if (fileSystem.FileExists(filePath.ChangeExtension("nam")))
                namStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension("nam"));

            _files = _hed.Load(hedStream, mrgStream, namStream);
        }
    }
}
