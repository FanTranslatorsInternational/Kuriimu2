using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5._3DS.Images
{
    class ZtexState : IImageState, ILoadFiles, ISaveFiles
    {
        private Ztex _img;
        private List<IImageInfo> _images;

        public IReadOnlyList<IImageInfo> Images { get; private set; }
        public bool ContentChanged => _images.Any(x => x.ContentChanged);

        public ZtexState()
        {
            _img = new Ztex();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var encDef = ZtexSupport.GetEncodingDefinition();

            Images = _images = _img.Load(fileStream).Select(x => new KanvasImageInfo(encDef, x)).Cast<IImageInfo>().ToList();
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _images.Select(x => x.ImageData).ToArray());

            return Task.CompletedTask;
        }
    }
}
