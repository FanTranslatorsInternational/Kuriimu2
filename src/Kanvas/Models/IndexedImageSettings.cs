using Kanvas.Interface;

namespace Kanvas.Models
{
    /// <summary>
    /// Defines additional settings with which an index based image will be loaded, quantized and saved.
    /// </summary>
    public class IndexedImageSettings : ImageSettings
    {
        /// <summary>
        /// The encoding for the index based image.
        /// </summary>
        public IIndexEncoding IndexEncoding { get; }

        /// <inheritdoc cref="QuantizationSettings"/>
        public QuantizationSettings QuantizationSettings { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="IndexedImageSettings"/>.
        /// </summary>
        /// <param name="indexEncoding">The encoding used for the index data.</param>
        /// <param name="paletteEncoding">The encoding used for the colors in the palette.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        public IndexedImageSettings(IIndexEncoding indexEncoding, IColorEncoding paletteEncoding, int width, int height) : base(paletteEncoding, width, height)
        {
            IndexEncoding = indexEncoding;
        }
    }
}
