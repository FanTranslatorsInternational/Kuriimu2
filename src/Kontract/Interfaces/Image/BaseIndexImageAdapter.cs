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
        protected abstract Bitmap Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding);

        #region BaseImageAdapter

        public override Task<TranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            return TranscodeImage(info, imageEncoding, null, progress);
        }

        public override bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding)
        {
            return Commit(info, image, imageEncoding, null, null);
        }

        #endregion

        public IList<EncodingInfo> PaletteEncodingInfos { get; }
        public Task<TranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, EncodingInfo paletteEncoding, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (!ImageEncodingInfos.Contains(imageEncoding))
                throw new ArgumentException(nameof(imageEncoding));
            if (paletteEncoding != null && !PaletteEncodingInfos.Contains(paletteEncoding))
                throw new ArgumentException(nameof(paletteEncoding));

            if (paletteEncoding == null)
            {
                // Image encoding changes
            }
            throw new NotImplementedException();
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

            if (info.ImageEncoding.IsIndexed != imageEncoding.IsIndexed)
            {
                if (imageEncoding.IsIndexed && palette == null) throw new ArgumentNullException(nameof(palette));
                if (imageEncoding.IsIndexed && paletteEncoding == null) throw new ArgumentNullException(nameof(paletteEncoding));

                var infoIndex = BitmapInfos.IndexOf(info);
                BitmapInfos[infoIndex] = imageEncoding.IsIndexed ?
                    new IndexedBitmapInfo(image, imageEncoding, palette, paletteEncoding) :
                    new BitmapInfo(image, imageEncoding);
                if (imageEncoding.IsIndexed)
                {
                    BitmapInfos[infoIndex] = new IndexedBitmapInfo(image, imageEncoding, palette, paletteEncoding);
                }
                else
                {
                    BitmapInfos[infoIndex] = new BitmapInfo(image, imageEncoding);
                }
            }

            info.Image = image;
            info.ImageEncoding = imageEncoding;
            if (info is IndexedBitmapInfo indexInfo)
                indexInfo.PaletteEncoding = paletteEncoding;

            if (imageEncoding.IsIndexed)
                ;

            throw new NotImplementedException();
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
            return TranscodeImage(info, info.ImageEncoding, info.PaletteEncoding, progress);
        }

        public Task<TranscodeResult> SetColorInPalette(IndexedBitmapInfo info, int index, Color color, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (!BitmapInfos.Contains(info))
                throw new ArgumentException(nameof(info));
            if (index < 0 || index >= info.ColorCount) throw new ArgumentOutOfRangeException(nameof(index));

            info.Palette[index] = color;
            return TranscodeImage(info, info.ImageEncoding, info.PaletteEncoding, progress);
        }

        //public abstract IList<BitmapInfo> BitmapInfos { get; }
        //public abstract IList<EncodingInfo> ImageEncodingInfos { get; }
        //public Task<TranscodeResult> TranscodeImage(Bitmap image, EncodingInfo newEncoding, IProgress<ProgressReport> progress)
        //{
        //    throw new NotImplementedException();
        //}

        //protected abstract Bitmap Transcode(Bitmap image, EncodingInfo newEncoding);

        //protected abstract Bitmap IndexedTranscode(Bitmap image, EncodingInfo newEncoding, IList<Color> palette,
        //    EncodingInfo paletteEncoding);

        //public bool Commit(Bitmap image, EncodingInfo encodingInfo, int bitmapInfoIndex)
        //{
        //    if (bitmapInfoIndex < 0) throw new ArgumentOutOfRangeException(nameof(bitmapInfoIndex));
        //    if (bitmapInfoIndex >= BitmapInfos.Count) throw new ArgumentOutOfRangeException(nameof(bitmapInfoIndex));

        //    BitmapInfos[bitmapInfoIndex].Image = image ?? throw new ArgumentNullException(nameof(image));
        //    BitmapInfos[bitmapInfoIndex].ImageEncoding = encodingInfo ?? throw new ArgumentNullException(nameof(encodingInfo));

        //    return true;
        //}

        //public IList<EncodingInfo> PaletteEncodingInfos { get; }
        //public Task<TranscodeResult> SetPalette(int bitmapInfoIndex, IList<Color> palette, IProgress<ProgressReport> progress)
        //{
        //    if (bitmapInfoIndex >= BitmapInfos.Count) throw new ArgumentOutOfRangeException(nameof(bitmapInfoIndex));
        //    if (!(BitmapInfos[bitmapInfoIndex] is IndexedBitmapInfo indexBitmapInfo)) throw new InvalidOperationException("Only IndexedBitmapInfos allowed.");
        //    if (palette == null) throw new ArgumentNullException(nameof(palette));
        //    if (palette.Count <= 0) throw new ArgumentOutOfRangeException(nameof(palette.Count));
        //    // TODO: Don't allow different color counts yet
        //    if (palette.Count != indexBitmapInfo.ColorCount) throw new InvalidOperationException("Differing color counts not allowed.");

        //    indexBitmapInfo.Palette = palette;
        //    return TranscodeImage(indexBitmapInfo.Image, indexBitmapInfo.ImageEncoding, progress);
        //}

        //public Task<TranscodeResult> SetColorInPalette(int bitmapInfoIndex, int paletteIndex, Color newColor, IProgress<ProgressReport> progress)
        //{
        //    if (bitmapInfoIndex >= BitmapInfos.Count) throw new ArgumentOutOfRangeException(nameof(bitmapInfoIndex));
        //    if (!(BitmapInfos[bitmapInfoIndex] is IndexedBitmapInfo indexBitmapInfo)) throw new InvalidOperationException("Only IndexedBitmapInfos allowed.");
        //    if (paletteIndex < 0 || paletteIndex >= indexBitmapInfo.ColorCount) throw new ArgumentOutOfRangeException(nameof(paletteIndex));

        //    indexBitmapInfo.Palette[paletteIndex] = newColor;
        //    return TranscodeImage(indexBitmapInfo.Image, indexBitmapInfo.ImageEncoding, progress);
        //}
    }
}
