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

//namespace plugin_nintendo.Archives
//{
//    public class BresPlugin : IFilePlugin, IIdentifyFiles
//    {
//        public Guid PluginId => Guid.Parse("c9e2bcda-9d62-49a9-8440-55d31310faaf");
//        public PluginType PluginType => PluginType.Archive;
//        public string[] FileExtensions => new[] {"*.bres"};
//        public PluginMetadata Metadata { get; }

//        public BresPlugin()
//        {
//            Metadata=new PluginMetadata("BRRES","onepiecefreak","The main resource format for NW4R.");
//        }

//        public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, IdentifyContext identifyContext)
//        {
//            var fileStream = await fileSystem.OpenFileAsync(filePath);

//            using var br=new BinaryReaderX(fileStream);
//            return br.ReadString(4) == "bres";
//        }

//        public IPluginState CreatePluginState(IBaseFileManager pluginManager)
//        {
//            return new BresState();
//        }
//    }
//}
