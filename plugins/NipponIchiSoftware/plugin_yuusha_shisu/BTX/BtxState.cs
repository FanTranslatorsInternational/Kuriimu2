using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_yuusha_shisu.BTX
{
    public class BtxState : IImageState, ILoadFiles
    {
        private readonly BTX _btx;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => BtxSupport.Encodings;
        public IDictionary<int, (IColorIndexEncoding, IList<int>)> SupportedIndexEncodings => BtxSupport.IndexEncodings;
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings => BtxSupport.PaletteEncodings;

        public BtxState()
        {
            _btx = new BTX();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _btx.Load(fileStream) };
        }

        public ImageInfo ConvertToImageInfo(IndexImageInfo indexImageInfo)
        {
            return new ImageInfo
            {
                ImageData = indexImageInfo.ImageData,
                ImageFormat = indexImageInfo.ImageFormat,
                ImageSize = indexImageInfo.ImageSize,
                Configuration = indexImageInfo.Configuration,
                Name = indexImageInfo.Name,
                MipMapData = indexImageInfo.MipMapData
            };
        }

        public IndexImageInfo ConvertToIndexImageInfo(ImageInfo imageInfo, int paletteFormat, byte[] paletteData)
        {
            return new IndexImageInfo
            {
                ImageData = imageInfo.ImageData,
                ImageFormat = imageInfo.ImageFormat,
                ImageSize = imageInfo.ImageSize,
                Configuration = imageInfo.Configuration,
                Name = imageInfo.Name,
                MipMapData = imageInfo.MipMapData,

                PaletteData = paletteData,
                PaletteFormat = paletteFormat
            };
        }
    }
}
