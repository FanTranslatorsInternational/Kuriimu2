using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.Context;
using Kontract.Models.IO;
using Kontract.Models.Text;

namespace plugin_yuusha_shisu.TALK
{
    class TalkState : ITextState, ILoadFiles, ISaveFiles
    {
        private readonly TALK _talk;

        public IList<TextEntry> Texts { get; private set; }

        public bool ContentChanged { get; set; }

        public TalkState()
        {
            _talk = new TALK();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var text= _talk.Load(fileStream);

            Texts = new[] { text };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var output = fileSystem.OpenFile(savePath, FileMode.Create);
            _talk.Save(output, Texts[0]);

            return Task.CompletedTask;
        }
    }
}
