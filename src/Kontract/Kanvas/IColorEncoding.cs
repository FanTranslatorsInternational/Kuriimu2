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
        /// The number of bits one pixel or block takes in the format specification.
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

        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <param name="taskCount">The number of tasks to use.</param>
        /// <returns>Decoded list of colors.</returns>
        IEnumerable<Color> Load(byte[] input, int taskCount);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <param name="taskCount">The number of tasks to use.</param>
        /// <returns>Encoded data.</returns>
        byte[] Save(IEnumerable<Color> colors, int taskCount);
    }
}
