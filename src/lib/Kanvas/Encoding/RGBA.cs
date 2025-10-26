using Kanvas.Encoding.Base;
using Kanvas.Encoding.Descriptors;
using Komponent.Contract.Enums;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the Rgba encoding.
    /// </summary>
    public class Rgba : PixelEncoding
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Rgba"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public Rgba(int r, int g, int b, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            this(r, g, b, "RGBA", byteOrder, bitOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Rgba"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public Rgba(int r, int g, int b, int a, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            this(r, g, b, a, "RGBA", byteOrder, bitOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Rgba"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public Rgba(int r, int g, int b, string componentOrder, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            this(r, g, b, 0, componentOrder, byteOrder, bitOrder)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Rgba"/>.
        /// </summary>
        /// <param name="r">Value of the red component.</param>
        /// <param name="g">Value of the green component.</param>
        /// <param name="b">Value of the blue component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the color components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public Rgba(int r, int g, int b, int a, string componentOrder, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            base(new RgbaPixelDescriptor(componentOrder, r, g, b, a), byteOrder, bitOrder)
        {
        }
    }
}
