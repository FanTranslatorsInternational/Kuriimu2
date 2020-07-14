using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kontract.Models.Text;

namespace plugin_yuusha_shisu.MSG
{
    public class MsgState : IPluginState, ITextState, ILoadFiles, ISaveFiles
    {
        private readonly MSG _msg;

        public IList<TextEntry> Texts { get; private set; }

        public bool ContentChanged { get; set; }

        public MsgState()
        {
            _msg = new MSG();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Texts = new[] { _msg.Load(fileStream) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _msg.Save(output, Texts[0]);

            return Task.CompletedTask;
        }
    }
}
