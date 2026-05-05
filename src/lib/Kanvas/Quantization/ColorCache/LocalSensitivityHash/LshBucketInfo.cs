using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Quantization.ColorCache.LocalSensitivityHash
{
    internal class LshBucketInfo
    {
        public SortedDictionary<int, Rgba32> Colors = [];

        /// <summary>
        /// Adds the color to the bucket information.
        /// </summary>
        /// <param name="paletteIndex">Index of the palette.</param>
        /// <param name="color">The color.</param>
        public void AddColor(int paletteIndex, Rgba32 color)
        {
            Colors.Add(paletteIndex, color);
        }
    }
}
