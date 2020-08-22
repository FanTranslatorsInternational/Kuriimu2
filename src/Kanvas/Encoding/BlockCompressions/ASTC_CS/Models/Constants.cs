using System.Drawing;

namespace Kanvas.Encoding.BlockCompressions.ASTC_CS.Models
{
    static class Constants
    {
        public static Color ErrorValue = Color.Magenta;

        public const int MaxWeightsPerBlock = 64;
        public const int MinWeightBitsPerBlock = 24;
        public const int MaxWeightBitsPerBlock = 96;
        public const int PartitionBits=10;
    }
}
