using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to quantize an image.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Quantizes an images.
        /// </summary>
        /// <param name="image">The image to quantize.</param>
        IEnumerable<int> Process(Bitmap image);

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">The collection of colors to quantize.</param>
        IEnumerable<int> Process(IEnumerable<Color> colors);

        /// <summary>
        /// Gets the index of the given color in the quantizer
        /// </summary>
        /// <param name="color"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        int GetPaletteIndex(Color color, int x, int y);

        /// <summary>
        /// Retrieves the palette created in the quantization process
        /// </summary>
        /// <returns></returns>
        IList<Color> GetPalette();

        /// <summary>
        /// Resets the instance to a new state.
        /// </summary>
        void Reset();
    }
}
