using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Exceptions.Image;
using Kontract.Models;
using Kontract.Models.Image;
using System.Drawing;

namespace Kontract.Interfaces.Image
{
    /// <summary>
    /// Base implementation for <see cref="IImageAdapter"/>.
    /// </summary>
    public abstract class BaseImageAdapter : IImageAdapter
    {
        /// <summary>
        /// Transcodes an image to a non-indexed encoding based on any method or library decided by the plugin.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="imageEncoding">The <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image.</returns>
        protected abstract Bitmap Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IImageAdapter.BitmapInfos"/>
        public abstract IList<BitmapInfo> BitmapInfos { get; }

        /// <inheritdoc cref="IImageAdapter.ImageEncodingInfos"/>
        public abstract IList<EncodingInfo> ImageEncodingInfos { get; }

        /// <summary>
        /// Transcodes an image using <see cref="Transcode(BitmapInfo,EncodingInfo,IProgress{ProgressReport})"/>.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="imageEncoding">The non-indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns><see cref="ImageTranscodeResult"/> which holds the transcoded information or the exception of this process.</returns>
        /// <remarks>Implements necessary null checks, exception catching and task handling.</remarks>
        public virtual Task<ImageTranscodeResult> TranscodeImage(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));
            if (imageEncoding.IsIndexed) throw new IndexedEncodingNotSupported(imageEncoding);

            // If encodings unchanged, don't transcode.
            if (bitmapInfo.ImageEncoding == imageEncoding)
                return Task.Factory.StartNew(() => new ImageTranscodeResult(bitmapInfo.Image));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var image = Transcode(bitmapInfo, imageEncoding, progress);
                    return new ImageTranscodeResult(image);
                }
                catch (Exception ex)
                {
                    return new ImageTranscodeResult(ex);
                }
            });
        }

        /// <inheritdoc cref="IImageAdapter.Commit(BitmapInfo,Bitmap,EncodingInfo)"/>
        public virtual bool Commit(BitmapInfo bitmapInfo, Bitmap image, EncodingInfo imageEncoding)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));
            if (imageEncoding.IsIndexed) throw new IndexedEncodingNotSupported(imageEncoding);

            bitmapInfo.Image = image ?? throw new ArgumentNullException(nameof(image));
            bitmapInfo.ImageEncoding = imageEncoding;

            return true;
        }
    }
}
