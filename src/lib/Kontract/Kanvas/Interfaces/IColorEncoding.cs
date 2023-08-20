using System.Collections.Generic;
using System.Drawing;
using Kontract.Kanvas.Models;

namespace Kontract.Kanvas.Interfaces
{
    /// <summary>
    /// An interface for defining a color encoding to use in the Kanvas image library.
    /// </summary>
    public interface IColorEncoding : IEncodingInfo
    {
        /// <summary>
        /// Decodes image data to a list of colors.
        /// </summary>
        /// <param name="input">Image data to decode.</param>
        /// <param name="loadContext">The context for the load operation.</param>
        /// <returns>Decoded list of colors.</returns>
        IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <param name="saveContext">The context for the save operation.</param>
        /// <returns>Encoded data.</returns>
        byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext);
    }
}
