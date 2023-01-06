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
    class AifState : IImageState, ILoadFiles, ISaveFiles
    {
        private Aif _img;
        private List<IImageInfo> _images;

        public IReadOnlyList<IImageInfo> Images { get; private set; }

        public bool ContentChanged => _images.Any(x => x.ContentChanged);

        public AifState()
        {
            _img = new Aif();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var encDef = AifSupport.GetEncodingDefinition();

            Images = _images = new List<IImageInfo> { new KanvasImageInfo(encDef, _img.Load(fileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _images[0].ImageData);

            return Task.CompletedTask;
        }
    }
}
