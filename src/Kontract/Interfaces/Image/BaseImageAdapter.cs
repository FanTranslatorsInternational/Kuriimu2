using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Kontract.Models;
using Kontract.Models.Image;

namespace Kontract.Interfaces.Image
{
    public abstract class BaseImageAdapter : IImageAdapter
    {
        protected abstract Bitmap Transcode(BitmapInfo bitmapInfo, EncodingInfo imageEncoding);

        public abstract IList<BitmapInfo> BitmapInfos { get; }
        public abstract IList<EncodingInfo> ImageEncodingInfos { get; }
        public virtual Task<TranscodeResult> TranscodeImage(BitmapInfo info, EncodingInfo imageEncoding, IProgress<ProgressReport> progress)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var newImg = Transcode(info, imageEncoding);
                    return new TranscodeResult(true, newImg);
                }
                catch (Exception e)
                {
                    return new TranscodeResult(false, e);
                }
            });
        }

        public virtual bool Commit(BitmapInfo info, Bitmap image, EncodingInfo imageEncoding)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (imageEncoding == null) throw new ArgumentNullException(nameof(imageEncoding));
            if (!ImageEncodingInfos.Contains(imageEncoding)) throw new ArgumentException(nameof(imageEncoding));

            info.Image = image ?? throw new ArgumentNullException(nameof(image));
            info.ImageEncoding = imageEncoding;

            return true;
        }
    }
}
