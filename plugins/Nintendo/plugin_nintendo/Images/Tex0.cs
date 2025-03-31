using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Tex0
    {
        private Tex0File _tex0;
        private Plt0File _plt0;

        public ImageFileInfo Load(Stream texStream, Stream pltStream)
        {
            using var br = new BinaryReaderX(texStream);

            // Read TEX0
            _tex0 = new Tex0File(texStream);

            // Read PLT0
            if (pltStream != null)
                _plt0 = new Plt0File(pltStream);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = _tex0.BitDepth,
                ImageData = _tex0.ImageData,
                ImageFormat = _tex0.Header.format,
                ImageSize = new Size(_tex0.Header.width, _tex0.Header.height),
                MipMapData = _tex0.MipData,
                RemapPixels = context => new RevolutionSwizzle(context)
            };

            if (_plt0 != null)
            {
                imageInfo.PaletteData = _plt0.PaletteData;
                imageInfo.PaletteFormat = _plt0.Header.format;
            }

            return imageInfo;
        }

        public void Save(Stream texOutput, Stream pltStream, ImageFileInfo imageInfo)
        {
            // Update TEX0 File
            _tex0.ImageData = imageInfo.ImageData;

            if (imageInfo.MipMapData != null)
            {
                _tex0.MipData.Clear();
                foreach (var mipData in imageInfo.MipMapData)
                    _tex0.MipData.Add(mipData);
            }

            _tex0.Header.width = (short)imageInfo.ImageSize.Width;
            _tex0.Header.height = (short)imageInfo.ImageSize.Height;
            _tex0.Header.format = imageInfo.ImageFormat;
            _tex0.Header.imgCount = (imageInfo.MipMapData?.Count ?? 0) + 1;
            _tex0.Header.mipLevels = (imageInfo.MipMapData?.Count ?? 0);

            // Write TEX0 File
            _tex0.Write(texOutput);

            if (imageInfo.PaletteData is null)
                return;

            // Update PLT0 File
            _plt0 ??= new Plt0File();

            _plt0.PaletteData = imageInfo.PaletteData;
            _plt0.Header.format = imageInfo.PaletteFormat;

            // Write PLT0
            _plt0.Write(pltStream);
        }
    }
}
