using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    class Tex0State : IImageState, ILoadFiles, ISaveFiles
    {
        private Tex0 _img;

        public EncodingDefinition EncodingDefinition { get; }
        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public Tex0State()
        {
            _img = new Tex0();

            EncodingDefinition = Tex0Support.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var texPath = filePath;
            var pltPath = $"{filePath.GetDirectory()}/../Palettes(NW4R)/{filePath.GetName()}";

            var texStream = await fileSystem.OpenFileAsync(texPath);
            var pltStream = fileSystem.FileExists(pltPath) ? await fileSystem.OpenFileAsync(pltPath) : null;

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, _img.Load(texStream, pltStream)) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var texPath = savePath;
            var pltPath = $"{savePath.GetDirectory()}/../Palettes(NW4R)/{savePath.GetName()}";

            var texStream = fileSystem.OpenFile(texPath, FileMode.Create, FileAccess.Write);
            var pltStream = Images[0].ImageInfo.HasPaletteInformation ? fileSystem.OpenFile(pltPath, FileMode.Create, FileAccess.Write) : null;

            _img.Save(texStream, pltStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
