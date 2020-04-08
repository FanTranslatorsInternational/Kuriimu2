using System;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace plugin_skip_ltd.Archives
{
    public class QpPlugin : IFilePlugin
    {
        public Guid PluginId => Guid.Parse("410009a3-49ef-4356-b9be-a7685c4f786c");
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public QpPlugin()
        {
            Metadata = new PluginMetadata("QP", "onepiecefreak", "The main archive in Chibi Robo!");
        }

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new QpState();
        }
    }
}
