using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class ImageEncoder
    {
        public ImageRawData Encode(ImageData image)
        {
            return new ImageRawData
            {
                Version = image.Version,

                BitDepth = image.Image.ImageInfo.BitDepth,
                Format = image.Image.ImageInfo.ImageFormat,
                Width = image.Image.ImageInfo.ImageSize.Width,
                Height = image.Image.ImageInfo.ImageSize.Height,

                PaletteBitDepth = image.Image.ImageInfo.PaletteBitDepth,
                PaletteData = image.Image.ImageInfo.PaletteData,
                PaletteFormat = image.Image.ImageInfo.PaletteFormat,

                LegacyData = image.LegacyData,

                Data = image.Image.ImageInfo.ImageData,
                MipMapData = image.Image.ImageInfo.MipMapData?.ToArray() ?? []
            };
        }
    }
}
