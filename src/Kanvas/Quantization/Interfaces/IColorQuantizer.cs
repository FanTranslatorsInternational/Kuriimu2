using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kanvas.Quantization.Interfaces
{
    /// <summary>
    /// Describes methods to quantize a collection of colors.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Determines if a color cache is used.
        /// </summary>
        bool UsesColorCache { get; }

        /// <summary>
        /// Determines if the color count can be changed.
        /// </summary>
        bool UsesVariableColorCount { get; }

        /// <summary>
        /// Determines if alpha is supported for quantization.
        /// </summary>
        bool SupportsAlpha { get; }

        /// <summary>
        /// Determines if the quantizer allows parallel processing.
        /// </summary>
        bool AllowParallel { get; }

        /// <summary>
        /// The number of tasks used for quantization.
        /// </summary>
        int TaskCount { get; }

        /// <summary>
        /// Sets the color cache.
        /// </summary>
        /// <param name="colorCache">Color cache to set.</param>
        void SetColorCache(IColorCache colorCache);

        /// <summary>
        /// Sets the color count for the palette.
        /// </summary>
        /// <param name="colorCount">Color count to set.</param>
        void SetColorCount(int colorCount);

        /// <summary>
        /// Sets the count of tasks the quantizer uses for parallel processing.
        /// </summary>
        /// <param name="taskCount">Count of tasks.</param>
        void SetParallelTasks(int taskCount);

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">The collection of colors to quantize.</param>
        IEnumerable<int> Process(IEnumerable<Color> colors);

        /// <summary>
        /// Gets the index of the given color in the quantizer.
        /// </summary>
        /// <param name="color">Color to find in the quantizer.</param>
        /// <returns></returns>
        int GetPaletteIndex(Color color);

        /// <summary>
        /// Creates a palette out of a collection of colors.
        /// </summary>
        /// <param name="colors"></param>
        void CreatePalette(IEnumerable<Color> colors);

        /// <summary>
        /// Retrieves the palette created in the quantization process.
        /// </summary>
        /// <returns>The palette of the quantized color collection.</returns>
        IList<Color> GetPalette();
    }
}
