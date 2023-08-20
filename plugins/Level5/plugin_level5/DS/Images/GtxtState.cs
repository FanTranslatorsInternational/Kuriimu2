using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Kanvas.Interfaces;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace plugin_level5.DS.Images
{
    class GtxtState : IImageState, ILoadFiles, ISaveFiles
    {
        private Gtxt _img;
        private List<IImageInfo> _images;

        public IReadOnlyList<IImageInfo> Images { get; private set; }

        public bool ContentChanged => _images.Any(x => x.ContentChanged);

        public GtxtState()
        {
            _img = new Gtxt();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream ltFileStream;
            Stream lpFileStream;

            if (filePath.GetExtensionWithDot() == ".lt")
            {
                ltFileStream = await fileSystem.OpenFileAsync(filePath);
                lpFileStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".lp"));
            }
            else
            {
                ltFileStream = await fileSystem.OpenFileAsync(filePath.ChangeExtension(".lt"));
                lpFileStream = await fileSystem.OpenFileAsync(filePath);
            }

            var encDef= GtxtSupport.GetEncodingDefinition();

            Images =_images= new List<IImageInfo> { new KanvasImageInfo(encDef, _img.Load(ltFileStream, lpFileStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream ltFileStream;
            Stream lpFileStream;

            if (savePath.GetExtensionWithDot() == ".lt")
            {
                ltFileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
                lpFileStream = fileSystem.OpenFile(savePath.ChangeExtension(".lp"), FileMode.Create, FileAccess.Write);
            }
            else
            {
                ltFileStream = fileSystem.OpenFile(savePath.ChangeExtension(".lt"), FileMode.Create, FileAccess.Write);
                lpFileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            }

            _img.Save(ltFileStream, lpFileStream, _images[0].ImageData);

            return Task.CompletedTask;
        }
    }
}
