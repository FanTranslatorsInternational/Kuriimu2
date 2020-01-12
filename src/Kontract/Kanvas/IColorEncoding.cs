using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas
{
    /// <summary>
    /// An interface for defining a color encoding to use in the Kanvas image library.
    /// </summary>
    public interface IColorEncoding
    {
        /// <summary>
        /// The number of bits one pixel takes in the format definition.
        /// </summary>
        /// <remarks>Known as bits per pixel (bpp).</remarks>
        int BitDepth { get; }

        // TODO: Remove BlockCompression indicator?
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
        IEnumerable<Color> Load(byte[] input);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <returns>Encoded data.</returns>
        byte[] Save(IEnumerable<Color> colors);
    }
}
