using System;
using System.Collections.Generic;
using System.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
using Kontract.Models.Images;
using Kontract.Models.IO;

namespace plugin_nintendo.Images
{
    public class CtpkState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Ctpk _ctpk;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => Support.CtrFormat;
        public IDictionary<int, IColorIndexEncoding> SupportedIndexEncodings { get; }
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged { get; }

        public CtpkState()
        {
            _ctpk = new Ctpk();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = _ctpk.Load(fileStream);
        }

        public async void Save(IFileSystem fileSystem, UPath savePath)
        {
            var saveStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create);
            _ctpk.Save(Images, saveStream);
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
