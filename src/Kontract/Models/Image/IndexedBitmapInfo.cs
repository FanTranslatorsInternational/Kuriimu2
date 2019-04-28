using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Kontract.Models.Image
{
    /// <summary>
    /// Extended <see cref="BitmapInfo"/> to add palette information.
    /// </summary>
    public class IndexedBitmapInfo : BitmapInfo
    {
        /// <summary>
        /// The palette of the main image.
        /// </summary>
        [Browsable(false)]
        public IList<Color> Palette { get; set; }

        /// <summary>
        /// The count of colors in a palette.
        /// </summary>
        [Category("Properties")]
        [Description("The count of colors in a palette.")]
        [ReadOnly(true)]
        public int ColorCount => Palette?.Count ?? 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="formatInfo"></param>
        /// <param name="palette"></param>
        public IndexedBitmapInfo(Bitmap image, FormatInfo formatInfo, IList<Color> palette) : base(image, formatInfo)
        {
            Palette = palette;
        }
    }
}
