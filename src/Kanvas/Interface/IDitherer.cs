using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Kanvas.Interface
{
    /// <summary>
    /// An interface for defining a ditherer to use in the Kanvas image library
    /// </summary>
    public interface IDitherer
    {
        /// <summary>
        /// The Width of the image to dither
        /// </summary>
        int Width { set; }

        /// <summary>
        /// The Height of the image to dither
        /// </summary>
        int Height { set; }

        /// <summary>
        /// The name to display for this ditherer
        /// </summary>
        string DithererName { get; }

        /// <summary>
        /// Process a source with a down sampled image to dither the colors
        /// </summary>
        /// <param name="source">RGBA8888 source image</param>
        /// <param name="toDither">Downsampled version of source</param>
        /// <returns></returns>
        IEnumerable<Color> Process(IEnumerable<Color> toDither, IEnumerable<Color> target, List<Color> palette);
    }
}
