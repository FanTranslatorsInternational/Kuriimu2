using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Kontract.Exceptions.Image;
using Kontract.Models;
using Kontract.Models.Image;

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
        /// <param name="newImageEncoding">The <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image.</returns>
        protected abstract Bitmap Transcode(BitmapInfo bitmapInfo, EncodingInfo newImageEncoding, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IImageAdapter.BitmapInfos"/>
        public abstract IList<BitmapInfo> BitmapInfos { get; }

        /// <inheritdoc cref="IImageAdapter.ImageEncodingInfos"/>
        public abstract IList<EncodingInfo> ImageEncodingInfos { get; }

        /// <summary>
        /// Transcodes an image using <see cref="Transcode(BitmapInfo,EncodingInfo,IProgress{ProgressReport})"/>.
        /// </summary>
        /// <param name="info">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="newImageEncoding">The non-indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns><see cref="ImageTranscodeResult"/> which holds the transcoded information or the exception of this process.</returns>
        /// <remarks>Implements necessary null checks, exception catching and task handling.</remarks>
        public virtual Task<ImageTranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo newImageEncoding, IProgress<ProgressReport> progress)
        {
            // Validity check
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (newImageEncoding == null) throw new ArgumentNullException(nameof(newImageEncoding));
            if (!ImageEncodingInfos.Contains(newImageEncoding))
                throw new ArgumentException(nameof(newImageEncoding));
            if (newImageEncoding.IsIndexed)
                throw new IndexedEncodingNotSupported(newImageEncoding);

            // If encodings unchanged, don't transcode
            if (info.ImageEncoding == newImageEncoding)
                return Task.Factory.StartNew(() => new ImageTranscodeResult(info.Image));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    // Transcode image
                    var newImg = Transcode(info, newImageEncoding, progress);
                    return new ImageTranscodeResult(newImg);
                }
                catch (Exception e)
                {
                    return new ImageTranscodeResult(e);
                }
            });
        }

        /// <inheritdoc cref="IImageAdapter.Commit(BitmapInfo,Bitmap,EncodingInfo)"/>
        public virtual bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding)
        {
            // Validity check
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!ImageEncodingInfos.Contains(imageEncoding))
                throw new ArgumentException(nameof(imageEncoding));
            if (imageEncoding.IsIndexed)
                throw new IndexedEncodingNotSupported(imageEncoding);

            info.Image = image ?? throw new ArgumentNullException(nameof(image));
            info.ImageEncoding = imageEncoding;

            return true;
        }
    }
}
