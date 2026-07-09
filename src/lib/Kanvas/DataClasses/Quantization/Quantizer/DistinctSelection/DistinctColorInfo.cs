using Kanvas.Extensions;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.DataClasses.Quantization.Quantizer.DistinctSelection
{
    internal class DistinctColorInfo(Rgba32 color)
    {
        private const int Factor = 5000000;

        public int Count { get; private set; } = 1;

        public uint Color { get; } = color.PackedValue;

        public int Alpha => color.A;

        public int Hue { get; } = Convert.ToInt32(color.GetHue() * Factor);

        public int Saturation { get; } = Convert.ToInt32(color.GetSaturation() * Factor);

        public int Brightness { get; } = Convert.ToInt32(color.GetBrightness()* Factor);

        public DistinctColorInfo IncreaseCount()
        {
            Count++;
            return this;
        }
    }
}
