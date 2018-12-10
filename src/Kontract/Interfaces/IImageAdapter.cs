using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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
        IList<BitmapInfo> BitmapInfos { get; }

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
        public List<Bitmap> Bitmaps { get; set; }

        /// <summary>
        /// The number of mipmaps that this BitmapInfo has.
        /// </summary>
        [Category("Properties")]
        [ReadOnly(true)]
        public virtual int MipMapCount => Bitmaps?.Count ?? 0;

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
        public Size Size => Bitmaps.FirstOrDefault()?.Size ?? new Size();
    }
}
