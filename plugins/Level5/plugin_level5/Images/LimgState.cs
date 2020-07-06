using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Context;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_level5.Images
{
    class LimgState : IImageState, ILoadFiles, ISaveFiles
    {
        private Limg _limg;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings { get; }

        public IDictionary<int, (IIndexEncoding Encoding, IList<int> PaletteEncodingIndices)> SupportedIndexEncodings =>
            LimgSupport.LimgFormats;

        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings => LimgSupport.LimgPaletteFormats;

        public bool ContentChanged => IsChanged();

        public LimgState()
        {
            _limg = new Limg();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _limg.Load(fileStream) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _limg.Save(fileStream, Images[0]);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
