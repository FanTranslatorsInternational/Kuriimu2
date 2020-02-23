using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Configuration;

namespace Kontract.Models.Images
{
    /// <summary>
    /// Extended <see cref="ImageInfo"/> to add palette information.
    /// </summary>
    public class IndexedImageInfo : ImageInfo
    {
        /// <summary>
        /// The palette of the main image.
        /// </summary>
        public IList<Color> Palette { get; set; }

        /// <summary>
        /// The format in which the palette is encoded.
        /// </summary>
        public int PaletteFormat { get; set; }

        /// <summary>
        /// The count of colors in a palette.
        /// </summary>
        public int ColorCount => Palette?.Count ?? 0;

        public IndexedImageInfo(Bitmap image, Size imageSize, int imageFormat,
            IList<Color> palette, int paletteFormat,
            IImageConfiguration configuration) :
            base(image, imageSize, imageFormat, configuration)
        {
            ContractAssertions.IsNotNull(palette, nameof(palette));

            Palette = palette;
            PaletteFormat = paletteFormat;
        }
    }
}
