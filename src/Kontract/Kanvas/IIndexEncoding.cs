using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas
{
    /// <summary>
    /// An interface for defining a color encoding to use in the Kanvas image library.
    /// </summary>
    public interface IColorIndexEncoding
    {
        /// <summary>
        /// The number of bits one pixel takes in the format definition.
        /// </summary>
        /// <remarks>Known as bits per pixel (bpp).</remarks>
        int BitDepth { get; }

        /// <summary>
        /// Defines if an encoding is a block compression.
        /// </summary>
        bool IsBlockCompression { get; }

        /// <summary>
        /// The name to display for this encoding.
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <returns>Decoded list of colors.</returns>
        IEnumerable<Color> Load(byte[] input, IList<Color> palette);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <returns>Encoded data and palette.</returns>
        byte[] Save(IEnumerable<int> indeces, IList<Color> palette);
    }
}
