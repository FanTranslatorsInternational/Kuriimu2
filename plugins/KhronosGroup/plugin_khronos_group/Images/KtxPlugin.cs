using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Komponent.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace plugin_khronos_group.Images
{
    public class KtxPlugin : IFilePlugin, IIdentifyFiles, IRegisterAssembly
    {
        private static readonly IList<byte[]> SupportedMagics = new List<byte[]>
        {
            /* KTX 11 */ new byte[]{0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A}
        };

        public Guid PluginId => Guid.Parse("d25919cc-ac22-4f4a-94b2-b0f42d1123d4");
        public PluginType PluginType => PluginType.Image;
        public string[] FileExtensions => new[] { "*.ktx" };
        public PluginMetadata Metadata { get; }

        public KtxPlugin()
        {
            Metadata = new PluginMetadata("KTX", "Nominom; onepiecefreak", "The image resource by the Khronos Group.");
        }

        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            using var br = new BinaryReaderX(fileStream);
            var magic = br.ReadBytes(12);

            return SupportedMagics.Any(x => x.SequenceEqual(magic));
        }

        public IPluginState CreatePluginState(IFileManager pluginManager)
        {
            return new KtxState();
        }

        public void RegisterAssemblies(DomainContext context)
        {
            context.FromResource("plugin_khronos_group.Libs.BCnEncoder.dll");
        }
    }
}
