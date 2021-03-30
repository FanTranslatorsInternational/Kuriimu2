using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kanvas.Encoding;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    class GcBnrState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly GcBnr _bnr;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public GcBnrState()
        {
            _bnr = new GcBnr();

            EncodingDefinition = new EncodingDefinition();
            EncodingDefinition.AddColorEncoding(0, new Rgba(5, 5, 5, 1, "ABGR", ByteOrder.BigEndian));
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new IKanvasImage[] { new KanvasImage(EncodingDefinition, _bnr.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.ReadWrite);
            _bnr.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
