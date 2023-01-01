using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.Entry;
using Kontract.Models.FileSystem;

namespace Kore.Models.LoadInfo
{
    class PluginLoadInfo
    {
        public IFileSystem FileSystem { get; }

        public UPath FilePath { get; }

        public IFilePlugin Plugin { get; }

        public PluginLoadInfo(IFileSystem fileSystem, UPath filePath)
        {
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            FileSystem = fileSystem;
            FilePath = filePath;
        }

        public PluginLoadInfo(IFileSystem fileSystem, UPath filePath, IFilePlugin plugin) :
            this(fileSystem, filePath)
        {
            ContractAssertions.IsNotNull(plugin, nameof(plugin));

            Plugin = plugin;
        }
    }
}
