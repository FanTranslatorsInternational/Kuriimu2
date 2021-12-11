using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace plugin_square_enix.Archives
{
    public class BinPlugin : IFilePlugin, IIdentifyFiles
    {
        // The type of files this plugin represents
        public PluginType PluginType => PluginType.Archive;

        // This is the unique plugin id, with which a plugin can be identified in the framework
        public Guid PluginId => Guid.Parse("ed8873c8-a4e5-4e17-b1d2-b1c245ef52f3");

        // These file extensions allow for an additional identification of the format
        public string[] FileExtensions => new[] { "*.bin" };

        // Additional plugin meta data
        public PluginMetadata Metadata => new PluginMetadata(".bin", "Caleb Mabry; OnePieceFreak", "The archive file format found in 3DS");

        // Allows an identification of one or more files of the parent file system to identify
        // if the given file is the supported file format.
        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext context)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "pack";
        }

        // Creates the plugin state, which implements all the format specific actions and properties
        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
        {
            return new BinState();
        }
    }

}
