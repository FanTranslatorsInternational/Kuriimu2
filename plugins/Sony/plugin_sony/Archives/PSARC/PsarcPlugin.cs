using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_sony.Archives.PSARC
{
    /// <summary>
    /// PSARC Plugin
    /// </summary>
    public class PsarcPlugin : IFilePlugin
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
        public IPluginState CreatePluginState(IPluginManager pluginManager) => new PsarcState();
    }
}
