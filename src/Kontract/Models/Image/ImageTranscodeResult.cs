using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kontract.Models.Image
{
    /// <summary>
    /// Result object, holding necessary information for image transcoding.
    /// </summary>
    public class ImageTranscodeResult
    {
        /// <summary>
        /// Declares if the process finished successfully.
        /// </summary>
        public bool Result { get; }

        /// <summary>
        /// The resulting image.
        /// </summary>
        public Bitmap Image { get; }

        /// <summary>
        /// The resulting palette.
        /// </summary>
        public IList<Color> Palette { get; set; }

        /// <summary>
        /// Contains the various exceptions that may have occured.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a new <see cref="ImageTranscodeResult"/> with the given image.
        /// </summary>
        /// <param name="image"></param>
        public ImageTranscodeResult(Bitmap image)
        {
            Result = image != null;
            Image = image;
            if (image == null)
                Exception = new ArgumentNullException(nameof(image));
        }

        /// <summary>
        /// Creates a new <see cref="ImageTranscodeResult"/> with the given image and palette.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="palette"></param>
        public ImageTranscodeResult(Bitmap image, IList<Color> palette)
        {
            Result = image != null && palette != null;
            Image = image;
            Palette = palette;
            if (image == null && palette == null)
                Exception = new AggregateException(new ArgumentNullException(nameof(image)), new ArgumentNullException(nameof(palette)));
            else
                Exception = image == null ? new ArgumentNullException(nameof(image)) : new ArgumentNullException(nameof(palette));
        }

        /// <summary>
        /// Creates a new <see cref="ImageTranscodeResult"/> with the exception.
        /// </summary>
        /// <param name="ex"></param>
        public ImageTranscodeResult(Exception ex)
        {
            Result = false;
            Exception = ex ?? new AggregateException(ex, new ArgumentNullException(nameof(ex)));
        }
    }
}
