using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// PSARC Plugin
    /// </summary>
    public class PsarcPlugin : IFilePlugin, IIdentifyFiles
    {
        public Guid PluginId => Guid.Parse("A260C29A-323B-4725-9592-737544F77C65");
        public PluginType PluginType => PluginType.Archive;
        public string[] FileExtensions => new[] { "*.psarc" };
        public PluginMetadata Metadata { get; }

        /// <summary>
        /// PSARC Constructor
        /// </summary>
        public PsarcPlugin()
        {
            Metadata = new PluginMetadata("PSARC", "IcySon55", "The PlayStation archive format used on several platforms.");
        }

        /// <summary>
        /// PSARC State Creation
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public IPluginState CreatePluginState(IBaseFileManager pluginManager) => new PsarcState();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="filePath"></param>
        /// <param name="identifyContext"></param>
        /// <returns></returns>
        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var isPsarc = false;

            try
            {
                using var br = new BinaryReaderX(fileStream, ByteOrder.BigEndian);
                var header = br.ReadType<PsarcHeader>();
                isPsarc = header.Magic == "PSAR" && (header.Compression == "zlib" || header.Compression == "lzma");
            }
            catch (Exception) { }

            return isPsarc;
        }
    }
}
