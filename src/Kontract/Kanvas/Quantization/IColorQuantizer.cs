using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Kanvas.Quantization
{
    /// <summary>
    /// Describes methods to quantize a collection of colors.
    /// </summary>
    public interface IColorQuantizer
    {
        IColorCache ColorCache { get; }

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
        int TaskCount { get; set; }

        /// <summary>
        /// Quantizes a collection of colors.
        /// </summary>
        /// <param name="colors">The collection of colors to quantize.</param>
        IEnumerable<int> Process(IEnumerable<Color> colors);

        /// <summary>
        /// Creates a palette out of a collection of colors.
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        IList<Color> CreatePalette(IEnumerable<Color> colors);
    }
}
