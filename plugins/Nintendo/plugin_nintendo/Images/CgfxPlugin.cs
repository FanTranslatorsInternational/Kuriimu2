//using System;
//using System.Threading.Tasks;
//using Komponent.IO;
//using Kontract.Interfaces.FileSystem;
//using Kontract.Interfaces.Managers;
//using Kontract.Interfaces.Plugins.Identifier;
//using Kontract.Interfaces.Plugins.State;
//using Kontract.Models;
//using Kontract.Models.Context;
//using Kontract.Models.IO;

//namespace plugin_nintendo.Images
//{
//    // HINT: Current status of plugin
//    // Cannot save at all
//    // Can open TXOB's that do not contain user data
//    // Tries to either parse data of a DICT or read it as a byte[], which can currently lead to problems
//    public class CgfxPlugin : IFilePlugin, IIdentifyFiles
//    {
//        public Guid PluginId => Guid.Parse("9238adb8-af88-43cc-a380-bcb50776b193");
//        public PluginType PluginType => PluginType.Image;
//        public string[] FileExtensions => new[] { "*.cgfx", "*.bctex", "*.bcmdl", "*.bcres" };
//        public PluginMetadata Metadata { get; }

//        public CgfxPlugin()
//        {
//            Metadata = new PluginMetadata("CGFX", "onepiecefreak", "A texture object resource in Nintendo games.");
//        }

//        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
//        {
//            var fileStream = await fileSystem.OpenFileAsync(filePath);

//            using var br=new BinaryReaderX(fileStream);
//            return br.ReadString(4) == "CGFX";
//        }

//        public IPluginState CreatePluginState(IPluginManager pluginManager)
//        {
//            return new CgfxState();
//        }
//    }
//}
