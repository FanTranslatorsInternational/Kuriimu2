using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;
using EncodingInfo = Kontract.Models.Image.EncodingInfo;

namespace Kontract.Interfaces.Image
{
    public abstract class BaseIndexImageAdapter : BaseImageAdapter, IIndexedImageAdapter
    {
        protected abstract (Bitmap newImg, IList<Color> palette) Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding);
        protected abstract Bitmap TranscodeWithPalette(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding);

        #region BaseImageAdapter

        public override Task<TranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            return TranscodeImage(info, imageEncoding, null, false, progress);
        }

        public override bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding)
        {
            return Commit(info, image, imageEncoding, null, null);
        }

        #endregion

        public abstract IList<EncodingInfo> PaletteEncodingInfos { get; }
        public Task<TranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, bool updatePalette, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (!ImageEncodingInfos.Contains(imageEncoding))
                throw new ArgumentException(nameof(imageEncoding));
            if (paletteEncoding == null && imageEncoding.IsIndexed)
                throw new ArgumentNullException(nameof(paletteEncoding));
            if (paletteEncoding != null && !PaletteEncodingInfos.Contains(paletteEncoding))
                throw new ArgumentException(nameof(paletteEncoding));

            if (updatePalette)
                return Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var newImg = TranscodeWithPalette(info, imageEncoding, (info as IndexedBitmapInfo)?.Palette, paletteEncoding);
                        return new TranscodeResult(true, newImg) { Palette = (info as IndexedBitmapInfo)?.Palette };
                    }
                    catch (Exception e)
                    {
                        return new TranscodeResult(false, e);
                    }
                });

            // If all encodings are unchanged, don't transcode
            if (paletteEncoding == null && info.ImageEncoding == imageEncoding)
                return Task.Factory.StartNew(() => new TranscodeResult(true, info.Image));
            if (info is IndexedBitmapInfo indexInfo && info.ImageEncoding == imageEncoding && indexInfo.PaletteEncoding == paletteEncoding)
                return Task.Factory.StartNew(() => new TranscodeResult(true, info.Image));

            // Image encoding changes
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    if (imageEncoding.IsIndexed)
                    {
                        var data = Transcode(info, imageEncoding, paletteEncoding);
                        return new TranscodeResult(true, data.newImg) { Palette = data.palette };
                    }

                    var newImg = Transcode(info, imageEncoding);
                    return new TranscodeResult(true, newImg);
                }
                catch (Exception e)
                {
                    return new TranscodeResult(false, e);
                }
            });
        }

        public virtual bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (!ImageEncodingInfos.Contains(imageEncoding))
                throw new ArgumentException(nameof(imageEncoding));
            if (paletteEncoding != null && !PaletteEncodingInfos.Contains(paletteEncoding))
                throw new ArgumentException(nameof(paletteEncoding));
            if (imageEncoding.IsIndexed && palette == null) throw new ArgumentNullException(nameof(palette));
            if (imageEncoding.IsIndexed && paletteEncoding == null) throw new ArgumentNullException(nameof(paletteEncoding));

            var indexInfo = info as IndexedBitmapInfo;

            // If formats are the same don't commit anything
            if (info.ImageEncoding == imageEncoding && indexInfo == null)
                return true;
            if (info.ImageEncoding == imageEncoding && indexInfo != null &&
                indexInfo.PaletteEncoding == paletteEncoding)
                return true;

            // If format changed from indexed to non-indexed or vice versa
            if (info.ImageEncoding.IsIndexed != imageEncoding.IsIndexed)
            {
                var infoIndex = BitmapInfos.IndexOf(info);
                BitmapInfos[infoIndex] = imageEncoding.IsIndexed ?
                    new IndexedBitmapInfo(image, imageEncoding, palette, paletteEncoding) :
                    new BitmapInfo(image, imageEncoding);
            }
            // If format changed without having its indexing to change
            else
            {
                info.Image = image;
                info.ImageEncoding = imageEncoding;
                if (indexInfo != null)
                {
                    indexInfo.Palette = palette;
                    indexInfo.PaletteEncoding = paletteEncoding;
                }
            }

            return true;
        }

        public Task<TranscodeResult> SetPalette(IndexedBitmapInfo info, IList<Color> palette, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            // TODO: Allow different color counts
            if (palette.Count != info.ColorCount) throw new ArgumentException(nameof(palette.Count));

            info.Palette = palette;
            return TranscodeImage(info, info.ImageEncoding, info.PaletteEncoding, true, progress);
        }

        public Task<TranscodeResult> SetColorInPalette(IndexedBitmapInfo info, int index, Color color, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (index < 0 || index >= info.ColorCount) throw new ArgumentOutOfRangeException(nameof(index));

            info.Palette[index] = color;
            return TranscodeImage(info, info.ImageEncoding, info.PaletteEncoding, true, progress);
        }
    }
}
