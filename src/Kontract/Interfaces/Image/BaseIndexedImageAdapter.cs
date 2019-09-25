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
    /// Base implementation for <see cref="IIndexedImageAdapter"/>.
    /// </summary>
    public abstract class BaseIndexedImageAdapter : BaseImageAdapter, IIndexedImageAdapter
    {
        /// <summary>
        /// Transcodes an image to an indexed encoding based on any method or library decided by the plugin.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="imageEncoding">The indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="paletteEncoding">The <see cref="EncodingInfo"/> to transcode the palette into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image and palette.</returns>
        // TODO: Add possibilities of giving quantization information.
        protected abstract (Bitmap Image, IList<Color> Palette) Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IIndexedImageAdapter.PaletteEncodingInfos"/>
        public abstract IList<EncodingInfo> PaletteEncodingInfos { get; }

        /// <summary>
        /// Transcodes an image using <see cref="Transcode(BitmapInfo,EncodingInfo,EncodingInfo,IProgress{ProgressReport})"/>.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="BitmapInfo"/> that holds all image information.</param>
        /// <param name="imageEncoding">The indexed <see cref="EncodingInfo"/> to transcode the image into.</param>
        /// <param name="paletteEncoding">The non-indexed <see cref="EncodingInfo"/> to transcode the palette into.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns><see cref="ImageTranscodeResult"/> which holds the transcoded information or the exception of this process.</returns>
        /// <remarks>Implements necessary null checks, exception catching and task handling.</remarks>
        // TODO: Add possibilities of giving quantization information.
        public virtual Task<ImageTranscodeResult> TranscodeImage(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, IProgress<ProgressReport> progress)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (paletteEncoding == null) throw new ArgumentNullException(nameof(paletteEncoding));
            if (!BitmapInfos.Contains(bitmapInfo)) throw new ArgumentException(nameof(bitmapInfo));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));
            if (!PaletteEncodingInfos.Contains(paletteEncoding)) throw new ArgumentException(nameof(paletteEncoding));
            if (!imageEncoding.IsIndexed) throw new EncodingNotSupported(imageEncoding);
            if (paletteEncoding.IsIndexed) throw new IndexedEncodingNotSupported(paletteEncoding);

            //// If encodings unchanged, don't transcode.
            //if (bitmapInfo.ImageEncoding == imageEncoding)
            //{
            //    if (!(bitmapInfo is IndexedBitmapInfo indexInfo))
            //        return Task.Factory.StartNew(() => new ImageTranscodeResult(bitmapInfo.Image));

            //    if (indexInfo.PaletteEncoding == paletteEncoding)
            //        return Task.Factory.StartNew(() => new ImageTranscodeResult(bitmapInfo.Image));
            //}

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var data = Transcode(bitmapInfo, imageEncoding, paletteEncoding, progress);
                    return new ImageTranscodeResult(data.Image, data.Palette);
                }
                catch (Exception ex)
                {
                    return new ImageTranscodeResult(ex);
                }
            });
        }

        #region Palette Operations

        /// <summary>
        /// Transcodes an image based on a new palette.
        /// </summary>
        /// <param name="bitmapInfo">The <see cref="IndexedBitmapInfo"/> that holds all image information.</param>
        /// <param name="palette">The new palette to apply.</param>
        /// <param name="progress"><see cref="IProgress{ProgressReport}"/> to report progress through.</param>
        /// <returns>Transcoded image</returns>
        protected abstract Bitmap TranscodeWithPalette(IndexedBitmapInfo bitmapInfo, IList<Color> palette, IProgress<ProgressReport> progress);

        /// <inheritdoc cref="IIndexedImageAdapter.SetPalette(IndexedBitmapInfo,IList{Color},IProgress{ProgressReport})"/>
        public virtual Task<ImageTranscodeResult> SetPalette(IndexedBitmapInfo bitmapInfo, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (!BitmapInfos.Contains(bitmapInfo)) throw new ArgumentException(nameof(bitmapInfo));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var image = TranscodeWithPalette(bitmapInfo, palette, progress);
                    return new ImageTranscodeResult(image, palette);
                }
                catch (Exception ex)
                {
                    return new ImageTranscodeResult(ex);
                }
            });
        }

        /// <inheritdoc cref="IIndexedImageAdapter.SetColorInPalette(IndexedBitmapInfo,int,Color,IProgress{ProgressReport})"/>
        public virtual Task<ImageTranscodeResult> SetColorInPalette(IndexedBitmapInfo bitmapInfo, int index, Color color, IProgress<ProgressReport> progress)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (index < 0 || index >= bitmapInfo.ColorCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (!BitmapInfos.Contains(bitmapInfo)) throw new ArgumentException(nameof(bitmapInfo));

            var newPalette = new Color[bitmapInfo.Palette.Count];
            bitmapInfo.Palette.CopyTo(newPalette, 0);
            newPalette[index] = color;

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var image = TranscodeWithPalette(bitmapInfo, newPalette, progress);
                    return new ImageTranscodeResult(image, newPalette);
                }
                catch (Exception ex)
                {
                    return new ImageTranscodeResult(ex);
                }
            });
        }

        /// <inheritdoc cref="IIndexedImageAdapter.SetIndexInImage(IndexedBitmapInfo,Point,int,IProgress{ProgressReport})"/>
        public virtual Task<ImageTranscodeResult> SetIndexInImage(IndexedBitmapInfo indexInfo, Point pointInImg, int newIndex, IProgress<ProgressReport> progress)
        {
            // Validity checks
            if (indexInfo == null) throw new ArgumentNullException(nameof(indexInfo));
            if (pointInImg.X >= indexInfo.Image.Width || pointInImg.Y >= indexInfo.Image.Height)
                throw new ArgumentOutOfRangeException(nameof(pointInImg));
            if (newIndex < 0 || newIndex >= indexInfo.ColorCount) throw new ArgumentOutOfRangeException(nameof(newIndex));
            if (!BitmapInfos.Contains(indexInfo)) throw new ArgumentException(nameof(indexInfo));

            var newColor = indexInfo.Palette[newIndex];

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    indexInfo.Image.SetPixel(pointInImg.X, pointInImg.Y, newColor);
                    return new ImageTranscodeResult(indexInfo.Image, indexInfo.Palette);
                }
                catch (Exception ex)
                {
                    return new ImageTranscodeResult(ex);
                }
            });
        }

        #endregion

        /// <inheritdoc cref="IIndexedImageAdapter.Commit(BitmapInfo,Bitmap,EncodingInfo,IList{Color},EncodingInfo)"/>
        public virtual bool Commit(BitmapInfo bitmapInfo, Bitmap image, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding)
        {
            // Validity checks
            if (bitmapInfo == null) throw new ArgumentNullException(nameof(bitmapInfo));
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!BitmapInfos.Contains(bitmapInfo)) throw new ArgumentException(nameof(bitmapInfo));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));
            if (!imageEncoding.IsIndexed) throw new EncodingNotSupported(imageEncoding);
            if (imageEncoding.IsIndexed)
            {
                if (palette == null) throw new ArgumentNullException(nameof(palette));
                if (paletteEncoding == null) throw new ArgumentNullException(nameof(paletteEncoding));
                if (!PaletteEncodingInfos.Contains(paletteEncoding)) throw new ArgumentException(nameof(paletteEncoding));
                if (paletteEncoding.IsIndexed) throw new IndexedEncodingNotSupported(paletteEncoding);
            }

            // If format changed from indexed to non-indexed or vice versa.
            if (bitmapInfo.ImageEncoding.IsIndexed != imageEncoding.IsIndexed)
            {
                BitmapInfos[BitmapInfos.IndexOf(bitmapInfo)] = imageEncoding.IsIndexed ?
                    new IndexedBitmapInfo(image, imageEncoding, palette, paletteEncoding) :
                    new BitmapInfo(image, imageEncoding);
            }
            else // If format changed without having its "type" changed.
            {
                bitmapInfo.Image = image;
                bitmapInfo.ImageEncoding = imageEncoding;
                if (bitmapInfo is IndexedBitmapInfo indexInfo)
                {
                    indexInfo.Palette = palette;
                    indexInfo.PaletteEncoding = paletteEncoding;
                }
            }

            return true;
        }
    }
}
