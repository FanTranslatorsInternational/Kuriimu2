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
    public class ImgcState : IImageState, ILoadFiles, ISaveFiles
    {
        private Imgc imgc;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings { get; private set; }
        public IDictionary<int, (IIndexEncoding Encoding, IList<int> PaletteEncodingIndices)> SupportedIndexEncodings
        {
            get;
        }

        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged => IsChanged();

        public ImgcState()
        {
            imgc = new Imgc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { imgc.Load(fileStream) };

            SupportedEncodings = ImgcSupport.DetermineFormatMapping(imgc.ImageFormat, imgc.BitDepth, loadContext.DialogManager);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            imgc.Save(fileStream, Images[0], SupportedEncodings);

            return Task.CompletedTask;
        }

        private bool IsChanged()
        {
            return Images.Any(x => x.ContentChanged);
        }
    }
}
