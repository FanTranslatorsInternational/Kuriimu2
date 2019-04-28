using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Image
{
    /// <summary>
    /// This is the indexed image interface to edit a palette of a given <see cref="IndexedBitmapInfo"/>.
    /// </summary>
    public interface IIndexedImageAdapter:IImageAdapter
    {
        /// <summary>
        /// Sets the whole palette.
        /// </summary>
        /// <param name="info">The image info to set the palette in.</param>
        /// <param name="palette">The palette to set.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the palette was set successfully, False otherwise.</returns>
        Task<bool> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress);

        /// <summary>
        /// Sets a single color in the palette.
        /// </summary>
        /// <param name="info">The image info to set the color in.</param>
        /// <param name="color">The color to set in the palette.</param>
        /// <param name="index">The index to set the color to in the palette.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the palette was set successfully, False otherwise.</returns>
        Task<bool> SetColorInPalette(IndexedBitmapInfo info, Color color, int index, IProgress<ProgressReport> progress);
    }
}
