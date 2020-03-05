using System;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;

namespace plugin_yuusha_shisu.TALK
{
    public class TalkPlugin : IFilePlugin/*, IIdentifyFiles*/
    {
        public Guid PluginId => Guid.Parse("161932f1-9152-45e8-a421-f84ac077bea4");
        public string[] FileExtensions => new[] { "*.bin" };
        public PluginMetadata Metadata { get; }

        public TalkPlugin()
        {
            Metadata = new PluginMetadata("TALK", "StorMyu", "Death of a Hero");
        }

        //public async Task<bool> IdentifyAsync(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        //{
        //    try
        //    {
        //        var fileStream = await fileSystem.OpenFileAsync(filePath);
        //        using (var br = new BinaryReaderX(fileStream))
        //        {
        //            var magic = br.ReadString(4);
        //            var fileSize = br.ReadInt32();
        //            return magic == "TEXT" && fileSize == br.BaseStream.Length;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        public IPluginState CreatePluginState(IPluginManager pluginManager)
        {
            return new TalkState();
        }
    }
}
