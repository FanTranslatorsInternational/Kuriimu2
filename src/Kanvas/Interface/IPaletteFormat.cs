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
    public interface IPaletteFormat : IImageFormat, IImageFormatKnownDimensions
    {
        /// <summary>
        /// The Quantizer to use for palette creation
        /// </summary>
        ColorQuantizer ColorQuantizer { get; set; }
        /// <summary>
        /// The PathProvider the quantizer uses to scan the pixels
        /// </summary>
        PathProvider PathProvider { get; set; }
        /// <summary>
        /// The ColorCache the quantizer uses to calculate color distances
        /// </summary>
        ColorCache ColorCache { get; set; }

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
    }
}
