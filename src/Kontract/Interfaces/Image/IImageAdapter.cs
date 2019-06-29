using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;
using System.Drawing;

namespace Kontract.Interfaces.Image
{
    /// <inheritdoc cref="IPlugin"/>
    /// <summary>
    /// This is the image adapter interface for creating image format plugins.
    /// </summary>
    public interface IImageAdapter : IPlugin
    {
        /// <summary>
        /// The list of images provided by the image adapter to the UI.
        /// </summary>
        IList<BitmapInfo> BitmapInfos { get; }

        /// <summary>
        /// The list of formats provided by the image adapter to change encoding of the image data.
        /// </summary>
        IList<EncodingInfo> ImageEncodingInfos { get; }

        /// <summary>
        /// Instructs the plugin to transcode a given image into a non-index based encoding.
        /// </summary>
        /// <param name="info">The <see cref="BitmapInfo"/>containing the image to be transcoded.</param>
        /// <param name="imageEncoding">The <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="progress">The <see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image and if the operation was successful.</returns>
        Task<ImageTranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, IProgress<ProgressReport> progress);

        /// <summary>
        /// Instructs the plugin to update the <see cref="BitmapInfo"/> accordingly with the new information.
        /// </summary>
        /// <param name="info">The <see cref="BitmapInfo"/> to be updated.</param>
        /// <param name="image">Image to commit.</param>
        /// <param name="imageEncoding"><see cref="EncodingInfo"/> the image is encoded in.</param>
        /// <returns>Is commitment successful.</returns>
        bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding);
    }
}
