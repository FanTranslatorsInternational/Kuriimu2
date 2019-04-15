using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Kontract.Interfaces.Image
{
    /// <summary>
    /// The base bitmap info class.
    /// </summary>
    public class BitmapInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="formatInfo"></param>
        public BitmapInfo(Bitmap image, FormatInfo formatInfo)
        {
            Image = image;
            FormatInfo = formatInfo;
        }

        /// <summary>
        /// The main image data.
        /// </summary>
        [Browsable(false)]
        public Bitmap Image { get; set; }

        /// <summary>
        /// The list of all mipmap data.
        /// </summary>
        [Browsable(false)]
        public List<Bitmap> MipMaps { get; set; } = new List<Bitmap>();

        /// <summary>
        /// The number of mipmaps that this BitmapInfo has.
        /// </summary>
        [Category("Properties")]
        [ReadOnly(true)]
        public virtual int MipMapCount => MipMaps?.Count ?? 0;

        /// <summary>
        /// The name of the main image.
        /// </summary>
        [Category("Properties")]
        [ReadOnly(true)]
        public string Name { get; set; }

        /// <summary>
        /// Returns the dimensions of the main image.
        /// </summary>
        [Category("Properties")]
        [Description("The dimensions of the image.")]
        public Size Size => Image?.Size ?? new Size();

        /// <summary>
        /// The image format information for encoding and decoding purposes
        /// </summary>
        [Browsable(false)]
        public FormatInfo FormatInfo { get; set; }
    }
}
