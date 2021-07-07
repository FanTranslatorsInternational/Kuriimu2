using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kontract.Models.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace plugin_sega.Text
{
    public class SharpMsgState : ITextState, ILoadFiles, ISaveFiles
    {
        private SharpMsg _msg;

        public IList<TextEntry> Texts { get; private set; }

        public bool ContentChanged { get; private set; }

        public SharpMsgState()
        {
            _msg = new SharpMsg();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var stream = await fileSystem.OpenFileAsync(filePath);
            Texts = _msg.Load(stream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var stream = await fileSystem.OpenFileAsync(savePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite);
            _msg.Save(Texts, stream);
        }
    }
}
