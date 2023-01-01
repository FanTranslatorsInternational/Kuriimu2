using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Models;

namespace Kontract.Kanvas.Interfaces
{
    /// <summary>
    /// An interface for defining a color encoding to use in the Kanvas image library.
    /// </summary>
    public interface IIndexEncoding : IEncodingInfo
    {
        /// <summary>
        /// The maximum number of colors possible with the index depth given.
        /// </summary>
        int MaxColors { get; }

        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <param name="palette">The palette for the input.</param>
        /// <param name="loadContext">The context for the load operation.</param>
        /// <returns>Decoded list of colors.</returns>
        IEnumerable<Color> Load(byte[] input, IList<Color> palette, EncodingLoadContext loadContext);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="indices">List of colors to encode.</param>
        /// <param name="palette">The palette for the input.</param>
        /// <param name="saveContext">The context for the save operation.</param>
        /// <returns>Encoded data and palette.</returns>
        byte[] Save(IEnumerable<int> indices, IList<Color> palette, EncodingSaveContext saveContext);
    }
}
