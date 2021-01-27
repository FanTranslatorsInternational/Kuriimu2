using Kanvas.Encoding.Base;
using Kanvas.Encoding.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the La encoding.
    /// </summary>
    public class La : PixelEncoding
    {
        /// <summary>
        /// Initializes a new instance of <see cref="La"/>.
        /// </summary>
        /// <param name="l">Value of the luminence component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public La(int l, int a, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            this(l, a, "LA", byteOrder, bitOrder)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="La"/>.
        /// </summary>
        /// <param name="l">Value of the luminence component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        /// <param name="bitOrder">The bit order in which bit values are read.</param>
        public La(int l, int a, string componentOrder, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst) :
            base(new LaPixelDescriptor(componentOrder, l, a), byteOrder, bitOrder)
        {
        }
    }
}
