using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Models.Image
{
    /// <summary>
    /// Result object, holding necessary information for image transcoding
    /// </summary>
    public class ImageTranscodeResult
    {
        /// <summary>
        /// Declares if the process was finished successfully.
        /// </summary>
        public bool Result { get; }

        /// <summary>
        /// The transcoded image of that process.
        /// </summary>
        public Bitmap TranscodedImage { get; }
        public IList<Color> Palette { get; set; }
        public Exception Exception { get; }

        public ImageTranscodeResult(Exception exc)
        {
            Result = false;
            Exception = exc ?? new AggregateException(exc, new ArgumentNullException(nameof(exc)));
        }

        public ImageTranscodeResult(Bitmap image)
        {
            Result = image != null;
            TranscodedImage = image;
            if (image == null)
                Exception = new ArgumentNullException(nameof(image));
        }

        public ImageTranscodeResult(Bitmap image, IList<Color> palette)
        {
            Result = image != null && palette != null;
            TranscodedImage = image;
            Palette = palette;
            if (image == null && palette == null)
                Exception = new AggregateException(new ArgumentNullException(nameof(image)), new ArgumentNullException(nameof(palette)));
            else
                Exception = image == null ? new ArgumentNullException(nameof(image)) : new ArgumentNullException(nameof(palette));
        }
    }
}
