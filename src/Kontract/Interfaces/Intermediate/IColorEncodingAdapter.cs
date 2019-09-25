using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Models;
using System.Drawing;

namespace Kontract.Interfaces.Intermediate
{
    /// <summary>
    /// Provides methods to decode and encode image data.
    /// </summary>
    public interface IColorEncodingAdapter : IIntermediate
    {
        /// <summary>
        /// The swizzle adapter to use for <see cref="Decode"/> and <see cref="Encode"/>.
        /// </summary>
        IImageSwizzleAdapter Swizzle { get; set; }

        /// <summary>
        /// Calculates the number of bytes needed to be allocated for given image dimensions.
        /// </summary>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <returns>Bytes needed to allocate an image of the given dimensions.</returns>
        int CalculateLength(int width, int height);

        /// <summary>
        /// Decodes image data to an image.
        /// </summary>
        /// <param name="imgData">Data to decode.</param>
        /// <param name="width">Width of the final image.</param>
        /// <param name="height">Height of the final image.</param>
        /// <param name="progress">Reports progress.</param>
        /// <returns>The decoded image.</returns>
        Task<Bitmap> Decode(byte[] imgData, int width, int height,IProgress<ProgressReport> progress);

        /// <summary>
        /// Encodes an image to byte data.
        /// </summary>
        /// <param name="img">Image to encode.</param>
        /// <param name="progress">Reports progress.</param>
        /// <returns>The encoded data.</returns>
        Task<byte[]> Encode(Bitmap img, IProgress<ProgressReport> progress);
    }
}
