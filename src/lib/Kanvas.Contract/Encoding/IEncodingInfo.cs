using Kanvas.Contract.DataClasses;

namespace Kanvas.Contract.Encoding
{
    /// <summary>
    /// And interface to define meta information for an encoding to use in the Kanvas library.
    /// </summary>
    public interface IEncodingInfo
    {
        /// <summary>
        /// The bit depth of each color channel in RGBA order.
        /// </summary>
        /// <remarks>If encoding does not support separate color channel bits, all values will be set to -1.</remarks>
        ColorChannelBitDepths ColorChannelBitDepths => ColorChannelBitDepths.Unknown;

        /// <summary>
        /// The number of bits one pixel takes in the format specification.
        /// </summary>
        int BitDepth { get; }

        /// <summary>
        /// The number of bits per read/written value.
        /// </summary>
        int BitsPerValue { get; }

        /// <summary>
        /// The number of colors per read/written value.
        /// </summary>
        int ColorsPerValue { get; }

        /// <summary>
        /// The name to display for this encoding.
        /// </summary>
        string FormatName { get; }
    }
}
