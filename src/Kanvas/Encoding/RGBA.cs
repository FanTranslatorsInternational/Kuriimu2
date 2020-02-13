using Kanvas.Encoding.Base;
using Kanvas.Encoding.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the RGBA encoding.
    /// </summary>
    public class RGBA : PixelEncoding
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            base(new RgbaPixelDescriptor(componentOrder,r,g,b,0), byteOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RGBA"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public RGBA(int r, int g, int b, int a, string componentOrder = "RGBA", ByteOrder byteOrder = ByteOrder.LittleEndian):
            base(new RgbaPixelDescriptor(componentOrder, r, g, b, a), byteOrder)
        {
        }
    }
}
