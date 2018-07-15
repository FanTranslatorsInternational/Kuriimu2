using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace Kanvas.Interface
{
    /// <summary>
    /// An interface for defining an image format to use in the Kanvas image library
    /// </summary>
    public interface IImageFormat
    {
        /// <summary>
        /// The number of bits one pixel takes in the format definition. Also known as bits per pixel (bpp)
        /// </summary>
        int BitDepth { get; }

        /// <summary>
        /// The name to display for this format
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Decodes image data to a list of colors
        /// </summary>
        /// <param name="input">Image data to decode</param>
        /// <returns>Decoded list of colors</returns>
        IEnumerable<Color> Load(byte[] input);
        /// <summary>
        /// Encodes a list of colors
        /// </summary>
        /// <param name="colors">List of colors to encode</param>
        /// <returns>Encoded data</returns>
        byte[] Save(IEnumerable<Color> colors);
    }

    /// <summary>
    /// An interface for additionally defining a Width and a Height to use in the format conversion
    /// </summary>
    public interface IImageFormatKnownDimensions : IImageFormat
    {
        /// <summary>
        /// The Width of the image to convert
        /// </summary>
        int Width { set; }

        /// <summary>
        /// The Height of the image to convert
        /// </summary>
        int Height { set; }
    }
}
