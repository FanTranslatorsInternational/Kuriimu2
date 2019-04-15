using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Image
{
    /// <summary>
    /// This is the palette image interface to edit a palette of a given <see cref="PaletteBitmapInfo"/>.
    /// </summary>
    public interface IPaletteImage
    {
        /// <summary>
        /// Sets the whole palette.
        /// </summary>
        /// <param name="info">The image info to set the palette in.</param>
        /// <param name="palette">The palette to set.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the palette was set successfully, False otherwise.</returns>
        Task<bool> SetPalette(PaletteBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress);

        /// <summary>
        /// Sets a single color in the palette.
        /// </summary>
        /// <param name="info">The image info to set the color in.</param>
        /// <param name="color">The color to set in the palette.</param>
        /// <param name="index">The index to set the color to in the palette.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the palette was set successfully, False otherwise.</returns>
        Task<bool> SetColorInPalette(PaletteBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress);
    }
}
