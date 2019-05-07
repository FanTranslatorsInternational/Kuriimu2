using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Kontract.Exceptions.Image;
using Kontract.Models;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Image
{
    public abstract class BaseIndexedImageAdapter : BaseImageAdapter, IIndexedImageAdapter
    {
        /// <summary>
        /// Transcodes an image to an indexed encoding based on any method or library decided by the plugin.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="newImageEncoding">The indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="newPaletteEncoding">The <see cref="EncodingInfo"/> to transcode the palette into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image and palette.</returns>
        // TODO: Add possibilities of giving quantization information
        protected abstract (Bitmap newImg, IList<Color> palette) TranscodeIndexed(BitmapInfo bitmapInfo,
            EncodingInfo newImageEncoding, EncodingInfo newPaletteEncoding, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IIndexedImageAdapter.PaletteEncodingInfos"/>
        public abstract IList<EncodingInfo> PaletteEncodingInfos { get; }

        /// <summary>
        /// Transcodes an image using <see cref="TranscodeIndexed(BitmapInfo,EncodingInfo,EncodingInfo,IProgress{ProgressReport})"/>.
        /// </summary>
        /// <param name="info">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="newImageEncoding">The indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="newPaletteEncoding">The non-indexed <see cref="EncodingInfo"/> to transcode the palette into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns><see cref="ImageTranscodeResult"/> which holds the transcoded information or the exception of this process.</returns>
        /// <remarks>Implements necessary null checks, exception catching and task handling.</remarks>
        // TODO: Add possibilities of giving quantization information
        public virtual Task<ImageTranscodeResult> TranscodeIndexedImage(BitmapInfo info, EncodingInfo newImageEncoding,
            EncodingInfo newPaletteEncoding, IProgress<ProgressReport> progress)
        {
            // Validity check
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (newImageEncoding == null) throw new ArgumentNullException(nameof(newImageEncoding));
            if (newPaletteEncoding == null) throw new ArgumentNullException(nameof(newPaletteEncoding));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (!ImageEncodingInfos.Contains(newImageEncoding))
                throw new ArgumentException(nameof(newImageEncoding));
            if (!PaletteEncodingInfos.Contains(newPaletteEncoding))
                throw new ArgumentException(nameof(newPaletteEncoding));
            if (!newImageEncoding.IsIndexed)
                throw new EncodingNotSupported(newImageEncoding);
            if (newPaletteEncoding.IsIndexed)
                throw new IndexedEncodingNotSupported(newPaletteEncoding);

            // If encodings unchanged, don't transcode
            if (info.ImageEncoding == newImageEncoding)
            {
                if (!(info is IndexedBitmapInfo indexInfo))
                    return Task.Factory.StartNew(() => new ImageTranscodeResult(info.Image));

                if (indexInfo.PaletteEncoding == newPaletteEncoding)
                    return Task.Factory.StartNew(() => new ImageTranscodeResult(info.Image));
            }

            // Image encoding changes
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    // Transcode image
                    var data = TranscodeIndexed(info, newImageEncoding, newPaletteEncoding, progress);
                    return new ImageTranscodeResult(data.newImg, data.palette);
                }
                catch (Exception e)
                {
                    return new ImageTranscodeResult(e);
                }
            });
        }

        /// <inheritdoc cref="IIndexedImageAdapter.Commit(BitmapInfo,Bitmap,EncodingInfo,IList{Color},EncodingInfo)"/>
        public virtual bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding)
        {
            // Validity check
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (!ImageEncodingInfos.Contains(imageEncoding))
                throw new ArgumentException(nameof(imageEncoding));
            if (!imageEncoding.IsIndexed)
                throw new EncodingNotSupported(imageEncoding);
            if (imageEncoding.IsIndexed)
            {
                if (palette == null) throw new ArgumentNullException(nameof(palette));
                if (paletteEncoding == null) throw new ArgumentNullException(nameof(paletteEncoding));
                if (!PaletteEncodingInfos.Contains(paletteEncoding))
                    throw new ArgumentException(nameof(paletteEncoding));
                if (paletteEncoding.IsIndexed)
                    throw new IndexedEncodingNotSupported(paletteEncoding);
            }

            // If format changed from indexed to non-indexed or vice versa
            if (info.ImageEncoding.IsIndexed != imageEncoding.IsIndexed)
            {
                var infoIndex = BitmapInfos.IndexOf(info);
                BitmapInfos[infoIndex] = imageEncoding.IsIndexed ?
                    new IndexedBitmapInfo(image, imageEncoding, palette, paletteEncoding) :
                    new BitmapInfo(image, imageEncoding);
            }
            // If format changed without having its "type" changed
            else
            {
                info.Image = image;
                info.ImageEncoding = imageEncoding;
                if (info is IndexedBitmapInfo indexInfo)
                {
                    indexInfo.Palette = palette;
                    indexInfo.PaletteEncoding = paletteEncoding;
                }
            }

            return true;
        }

        #region Palette operations

        /// <summary>
        /// Transcodes an image based on a new palette.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="IndexedBitmapInfo"/> that holds all image information.</param>
        /// <param name="newPalette">The new palette to apply.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image</returns>
        protected abstract Bitmap TranscodeWithPalette(IndexedBitmapInfo bitmapInfo, IList<Color> newPalette, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IIndexedImageAdapter.SetPalette(IndexedBitmapInfo,IList{Color},IProgress{ProgressReport})"/>
        public virtual Task<ImageTranscodeResult> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var newImg = TranscodeWithPalette(info, palette, progress);
                    return new ImageTranscodeResult(newImg, palette);
                }
                catch (Exception e)
                {
                    return new ImageTranscodeResult(e);
                }
            });
        }

        /// <inheritdoc cref="IIndexedImageAdapter.SetColorInPalette(IndexedBitmapInfo,int,Color,IProgress{ProgressReport})"/>
        public Task<ImageTranscodeResult> SetColorInPalette(IndexedBitmapInfo info, int index, Color color, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (index < 0 || index >= info.ColorCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));

            var newPalette = new Color[info.Palette.Count];
            info.Palette.CopyTo(newPalette, 0);
            newPalette[index] = color;

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var newImg = TranscodeWithPalette(info, newPalette, progress);
                    return new ImageTranscodeResult(newImg, newPalette);
                }
                catch (Exception e)
                {
                    return new ImageTranscodeResult(e);
                }
            });
        }

        #endregion
    }
}
