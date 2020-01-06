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
        IEnumerable<(int, Color)> Load(byte[] input);

        /// <summary>
        /// Retrieves a color from a palette based on an index and optional other values.
        /// </summary>
        /// <param name="indexColor"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        Color GetColorFromIndex((int, Color) indexColor, IList<Color> palette);

        /// <summary>
        /// Encodes a list of colors.
        /// </summary>
        /// <param name="colors">List of colors to encode.</param>
        /// <returns>Encoded data and palette.</returns>
        byte[] Save(IEnumerable<(int, Color)> indexColors);
    }
}
//    public interface IIndexEncoding
//    {
//        /// <summary>
//        /// The name of the index format.
//        /// </summary>
//        string FormatName { get; }

//        /// <summary>
//        /// Decodes image data to a list of colors.
//        /// </summary>
//        /// <param name="input">Index data to decode.</param>
//        /// <returns>Decoded collection of indices.</returns>
//        IEnumerable<Color> Load(byte[] input);

//        ///// <summary>
//        ///// Composes indices and a palette to a collection of colors.
//        ///// </summary>
//        ///// <param name="indices">Collection of <see cref="IndexData"/> to compose.</param>
//        ///// <param name="palette">Collection of colors to compose.</param>
//        ///// <returns>Composed collection of colors.</returns>
//        //IEnumerable<Color> Compose(IEnumerable<IndexData> indices, IList<Color> palette);

//        ///// <summary>
//        ///// Decomposes a collection of colors.
//        ///// </summary>
//        ///// <param name="colors">Collection of colors.</param>
//        ///// <returns>Decomposed collection of indices and palette.</returns>
//        //(IEnumerable<IndexData> indices, IList<Color> palette) Decompose(IEnumerable<Color> colors);

//        ///// <summary>
//        ///// Decomposes a collection of colors with a given palette.
//        ///// </summary>
//        ///// <param name="colors">Collection of colors.</param>
//        ///// <param name="palette">The palette to derive the indices from.</param>
//        ///// <returns>Decomposed collection of indices.</returns>
//        //IEnumerable<IndexData> DecomposeWithPalette(IEnumerable<Color> colors, IList<Color> palette);

//        ///// <summary>
//        ///// Quantizes a collection of colors.
//        ///// </summary>
//        ///// <param name="colors">Collection of colors.</param>
//        ///// <returns>Quantized collection of indices and palette.</returns>
//        //(IEnumerable<IndexData> indices, IList<Color> palette) Quantize(IEnumerable<Color> colors, QuantizationSettings settings);

//        /// <summary>
//        /// Encodes a collection of colors.
//        /// </summary>
//        /// <param name="colors">List of colors to encode.</param>
//        /// <returns>Encoded index data.</returns>
//        byte[] Save(IEnumerable<Color> colors);
//    }
//}
