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

namespace plugin_lemon_interactive.Archives
{
    /// <summary>
    /// 
    /// </summary>
    public class Dpk4Plugin : IFilePlugin, IIdentifyFiles
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid PluginId => Guid.Parse("17A65248-8CD3-4B29-B101-C82FFFCB1D4A");

        /// <summary>
        /// 
        /// </summary>
        public PluginType PluginType => PluginType.Archive;

        /// <summary>
        /// 
        /// </summary>
        public string[] FileExtensions => new[] { "*.dpk" };

        /// <summary>
        /// 
        /// </summary>
        public PluginMetadata Metadata { get; }

        /// <summary>
        /// 
        /// </summary>
        public Dpk4Plugin()
        {
            Metadata = new PluginMetadata("DPK4", "IcySon55", "An archive plugin for Project Earth: Starmageddon.");
        }

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
            using var br = new BinaryReaderX(fileStream);

            return br.ReadString(4) == "DPK4";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new Dpk4State();
        }
    }
}
