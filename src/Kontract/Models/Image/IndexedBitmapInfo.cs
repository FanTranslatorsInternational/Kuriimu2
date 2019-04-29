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
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageEncoding"></param>
        /// <param name="palette"></param>
        public IndexedBitmapInfo(Bitmap image, EncodingInfo imageEncoding, IList<Color> palette, EncodingInfo paletteEncoding) : base(image, imageEncoding)
        {
            Palette = palette;
            PaletteEncoding = paletteEncoding;
        }

        /// <summary>
        /// The palette of the main image.
        /// </summary>
        [Browsable(false)]
        public IList<Color> Palette { get; set; }

        /// <summary>
        /// The format in which the palette is encoded.
        /// </summary>
        [Browsable(false)]
        public EncodingInfo PaletteEncoding { get; private set; }

        /// <summary>
        /// The count of colors in a palette.
        /// </summary>
        [Category("Properties")]
        [Description("The count of colors in a palette.")]
        [ReadOnly(true)]
        public int ColorCount => Palette?.Count ?? 0;

        /// <summary>
        /// Sets <see cref="PaletteEncoding"/>.
        /// </summary>
        /// <param name="paletteEnc"></param>
        public virtual void SetPaletteEncoding(EncodingInfo paletteEnc)
        {
            PaletteEncoding = paletteEnc;
        }
    }
}
