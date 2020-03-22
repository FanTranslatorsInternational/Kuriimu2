using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_level5.Images
{
    class ImgcState : IImageState, ILoadFiles, ISaveFiles
    {
        private Imgc _imgc;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => ImgcSupport.ImgcFormats;
        public IDictionary<int, (IColorIndexEncoding Encoding, IList<int> PaletteEncodingIndices)> SupportedIndexEncodings
        {
            get;
        }

        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged { get; set; }

        public ImgcState()
        {
            _imgc = new Imgc();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _imgc.Load(fileStream) };
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create);
            _imgc.Save(fileStream, Images[0]);

            return Task.CompletedTask;
        }

        public ImageInfo ConvertToImageInfo(IndexImageInfo indexImageInfo)
        {
            throw new NotImplementedException();
        }

        public IndexImageInfo ConvertToIndexImageInfo(ImageInfo imageInfo, int paletteFormat, byte[] paletteData)
        {
            throw new NotImplementedException();
        }
    }
}
