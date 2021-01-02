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

namespace plugin_level5.DS.Images
{
    class LimgState : IImageState, ILoadFiles, ISaveFiles
    {
        private Limg _limg;

        public EncodingDefinition EncodingDefinition { get; }

        public IList<IKanvasImage> Images { get; private set; }

        public bool ContentChanged => IsChanged();

        public LimgState()
        {
            _limg = new Limg();

            EncodingDefinition = LimgSupport.LimgPaletteFormats.ToPaletteDefinition();
            EncodingDefinition.AddIndexEncodings(LimgSupport.LimgFormats);
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _limg.Load(fileStream);

            Images = new List<IKanvasImage> { new KanvasImage(EncodingDefinition, img) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _limg.Save(fileStream, Images[0].ImageInfo);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
