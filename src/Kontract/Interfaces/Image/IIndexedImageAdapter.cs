using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;
using System.Drawing;

namespace Kontract.Interfaces.Image
{
    /// <inheritdoc />
    /// <summary>
    /// This is the indexed image interface to edit a palette of a given <see cref="IndexedBitmapInfo"/>.
    /// </summary>
    public interface IIndexedImageAdapter : IImageAdapter
    {
        /// <summary>
        /// The list of formats provided by the image adapter to change encoding of the image data.
        /// </summary>
        IList<EncodingInfo> PaletteEncodingInfos { get; }

        /// <summary>
        /// Instructs the plugin to transcode a given image into an index based encoding.
        /// </summary>
        /// <param name="image">The image to be transcoded.</param>
        /// <param name="imageEncoding">The indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="paletteEncoding">The non-indexed <see cref="EncodingInfo"/> to transcode the palette into.</param>
        /// <param name="progress">The <see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image and if the operation was successful.</returns>
        Task<ImageTranscodeResult> TranscodeImage(BitmapInfo image, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, IProgress<ProgressReport> progress);

        /// <summary>
        /// Sets the whole palette.
        /// </summary>
        /// <param name="bitmapInfo">The image info to set the palette in.</param>
        /// <param name="palette">The palette to set.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the palette was set successfully, False otherwise.</returns>
        Task<ImageTranscodeResult> SetPalette(IndexedBitmapInfo bitmapInfo, IList<Color> palette, IProgress<ProgressReport> progress);

        /// <summary>
        /// Sets a single color in the palette.
        /// </summary>
        /// <param name="bitmapInfo">The image info to set the color in.</param>
        /// <param name="index">The index to set the color to in the palette.</param>
        /// <param name="color">The color to set in the palette.</param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns><see langword="true"/> if the palette was set successfully, <see langword="false"/> otherwise.</returns>
        Task<ImageTranscodeResult> SetColorInPalette(IndexedBitmapInfo bitmapInfo, int index, Color color, IProgress<ProgressReport> progress);

        /// <summary>
        /// Resets the index at a given point in the image.
        /// </summary>
        /// <param name="bitmapInfo">The image info to set the index in.</param>
        /// <param name="pointInImg">The point at which to set the index.</param>
        /// <param name="newIndex">The new index to set at <param name="pointInImg">.</param></param>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns><see langword="true"/> if the index was successfully set, <see langword="false"/> otherwise.</returns>
        Task<ImageTranscodeResult> SetIndexInImage(IndexedBitmapInfo bitmapInfo, Point pointInImg, int newIndex, IProgress<ProgressReport> progress);

        /// <summary>
        /// Instructs the plugin to update the <see cref="BitmapInfo"/> accordingly with the new information.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> to be updated.</param>
        /// <param name="image">Image to commit.</param>
        /// <param name="imageEncoding"><see cref="EncodingInfo"/> the image is encoded in.</param>
        /// <param name="palette">The palette to commit.</param>
        /// <param name="paletteEncoding"><see cref="EncodingInfo"/> the palette is encoded in.</param>
        /// <returns>Is commitment successful.</returns>
        bool Commit(BitmapInfo bitmapInfo, Bitmap image, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding);
    }
}
