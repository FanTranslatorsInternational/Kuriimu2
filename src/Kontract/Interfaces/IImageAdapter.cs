using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;

namespace Kontract.Interfaces
{
    /// <summary>
    /// This is the image adapter interface for creating image format plugins.
    /// </summary>
    public interface IImageAdapter
    {
        /// <summary>
        /// The list of images provided by the image adapter to the UI.
        /// </summary>
        IList<BitmapInfo> Bitmaps { get; }

        /// <summary>
        /// Instructs the plugin to encode the bitmaps and report progress as it goes.
        /// </summary>
        /// <param name="progress">The progress object to report progress through.</param>
        /// <returns>True if the bitmaps were successfully encoded, False otherwise.</returns>
        Task<bool> Encode(IProgress<ProgressReport> progress);
    }

    /// <summary>
    /// The base bitmap info class.
    /// </summary>
    public class BitmapInfo
    {
        /// <summary>
        /// The bitmap data.
        /// </summary>
        [Browsable(false)]
        public Bitmap Bitmap { get; set; }

        /// <summary>
        /// The name of the bitmap.
        /// </summary>
        [Category("Properties")]
        [ReadOnly(true)]
        public string Name { get; set; }

        /// <summary>
        /// Returns the dimensions of the bitmap.
        /// </summary>
        [Category("Properties")]
        [Description("The dimensions of the image.")]
        public Size Size => Bitmap?.Size ?? new Size();
    }
}
