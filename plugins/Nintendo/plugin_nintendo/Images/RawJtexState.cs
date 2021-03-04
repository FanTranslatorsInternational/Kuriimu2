using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    class RawJtexState : IImageState, ILoadFiles, ISaveFiles
    {
        private RawJtex _raw;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public RawJtexState()
        {
            _raw = new RawJtex();

            EncodingDefinition = RawJtexSupport.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _raw.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _raw.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
