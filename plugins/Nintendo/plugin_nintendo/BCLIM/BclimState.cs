using System;
using System.Collections.Generic;
using System.IO;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.BCLIM
{
    public class BclimState : IImageState, ILoadFiles, ISaveFiles
    {
        private readonly Bclim _bclim;

        public IList<ImageInfo> Images { get; private set; }
        public IDictionary<int, IColorEncoding> SupportedEncodings => ImageFormats.CtrFormats;
        public IDictionary<int, (IColorIndexEncoding, IList<int>)> SupportedIndexEncodings { get; }
        public IDictionary<int, IColorEncoding> SupportedPaletteEncodings { get; }

        public bool ContentChanged { get; }

        public BclimState()
        {
            _bclim = new Bclim();
        }

        public async void Load(IFileSystem fileSystem, UPath filePath, ITemporaryStreamProvider temporaryStreamProvider,
            IProgressContext progress)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = new List<ImageInfo> { _bclim.Load(fileStream) };
        }

        public async void Save(IFileSystem fileSystem, UPath savePath, IProgressContext progress)
        {
            var saveStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create);
            _bclim.Save(saveStream, Images[0]);
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
