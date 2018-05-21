using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kanvas.Interface
{
    /// <summary>
    /// An interface for defining a palette format to use in the Kanvas image library
    /// </summary>
    public interface IPaletteFormat
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
        /// Sets a list of colors as palette
        /// </summary>
        /// <param name="paletteData">Data to get decoded into the palette</param>
        /// <param name="paletteFormat">The format to decode the data with</param>
        void SetPalette(byte[] paletteData, IImageFormat paletteFormat);
        /// <summary>
        /// Sets a list of colors as palette
        /// </summary>
        /// <param name="paletteColors">The list of colors to set as the palette</param>
        void SetPalette(IEnumerable<Color> paletteColors);

        /// <summary>
        /// Decodes image data to a list of colors
        /// </summary>
        /// <param name="input">Image data to decode</param>
        /// <returns>Decoded list of colors</returns>
        IEnumerable<Color> Load(byte[] input);

        /// <summary>
        /// Get the data for the recreated palette
        /// </summary>
        /// <param name="paletteFormat">The format to encode the data with</param>
        /// <returns>Encoded palette data</returns>
        byte[] GetPalette(IImageFormat paletteFormat);
        /// <summary>
        /// Get a list of colors for the recreated palette
        /// </summary>
        /// <returns>Recreated palette as a list of colors</returns>
        IEnumerable<Color> GetPalette();

        /// <summary>
        /// Encodes a list of colors
        /// </summary>
        /// <param name="colors">List of colors to encode</param>
        /// <returns>Encoded data</returns>
        byte[] Save(IEnumerable<Color> colors, ColorDistance colorDistance);
    }

    public enum ColorDistance : byte
    {
        OnlyHUE,
        DirectDistance,
        HSVWeighting
    }
}
