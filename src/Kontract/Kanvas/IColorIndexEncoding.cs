using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas
{
    /// <summary>
    /// An interface for defining a color encoding to use in the Kanvas image library.
    /// </summary>
    public interface IColorIndexEncoding : IEncodingInfo
    {
        /// <summary>
        /// The maximum number of colors possible with the index depth given.
        /// </summary>
        int MaxColors { get; }

        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <returns>Decoded list of colors.</returns>
        IEnumerable<Color> Load(byte[] input, IList<Color> palette, int taskCount);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <returns>Encoded data and palette.</returns>
        byte[] Save(IEnumerable<int> indeces, IList<Color> palette, int taskCount);
    }
}
