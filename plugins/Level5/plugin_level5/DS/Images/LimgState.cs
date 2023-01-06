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
using Kontract.Models.Plugins.State.Image;

namespace plugin_level5.DS.Images
{
    class LimgState : IImageState, ILoadFiles, ISaveFiles
    {
        private Limg _limg;
        private List<IImageInfo> _images;

        public IReadOnlyList<IImageInfo> Images { get; private set; }

        public bool ContentChanged => _images.Any(x => x.ContentChanged);

        public LimgState()
        {
            _limg = new Limg();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _limg.Load(fileStream);

            var encDef = LimgSupport.LimgPaletteFormats.ToPaletteDefinition();
            encDef.AddIndexEncodings(LimgSupport.LimgFormats);

            Images =_images= new List<IImageInfo> { new KanvasImageInfo(encDef, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _limg.Save(fileStream, _images[0].ImageData);

            return Task.CompletedTask;
        }
    }
}
