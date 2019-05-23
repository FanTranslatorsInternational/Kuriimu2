using System;
using Kanvas.Quantization.ColorCaches;
using Kanvas.Quantization.Interfaces;
using Kanvas.Quantization.Models.ColorCache;

namespace Kanvas.Models
{
    /// <summary>
    /// Settings to define the process of quantization.
    /// </summary>
    public class QuantizationSettings
    {
        /// <summary>
        /// The width of the image to quantize.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the image to quantize.
        /// </summary>
        public int Height { get; }

        /// <inheritdoc cref="IColorDitherer"/>
        public IColorDitherer Ditherer { get; set; }

        /// <inheritdoc cref="IColorQuantizer"/>
        public IColorQuantizer Quantizer { get; }

        /// <inheritdoc cref="IColorCache"/>
        public IColorCache ColorCache { get; set; }

        /// <inheritdoc cref="Quantization.Models.ColorCache.ColorModel"/>
        public ColorModel ColorModel { get; set; }

        /// <summary>
        /// The value at which the alpha colors gets distinguished from opaque colors.
        /// </summary>
        public int AlphaThreshold { get; set; }

        /// <summary>
        /// The count of colors to quantize to. 
        /// </summary>
        public int ColorCount { get; set; }

        /// <summary>
        /// The count of tasks to use in the quantization process.
        /// </summary>
        public int ParallelCount { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="QuantizationSettings"/>.
        /// </summary>
        /// <param name="quantizer">The quantizer to use.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        public QuantizationSettings(IColorQuantizer quantizer, int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            Quantizer = quantizer ?? throw new ArgumentNullException(nameof(quantizer));

            Width = width;
            Height = height;

            Ditherer = null;
            ColorCache = new EuclideanDistanceColorCache();
            ColorModel = ColorModel.RGB;
            ColorCount = 256;
            ParallelCount = 8;
        }
    }
}
