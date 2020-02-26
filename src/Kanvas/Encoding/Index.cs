using Kanvas.Encoding.Base;
using Kanvas.Encoding.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the default index encoding.
    /// </summary>
    public class Index : PixelIndexEncoding
    {
        /// <summary>
        /// Creates a new instance of <see cref="Index"/>.
        /// </summary>
        /// <param name="i">Value of the index component.</param>
        /// <param name="componentOrder">The order of the components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public Index(int i, string componentOrder = "IA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            this(i, 0, componentOrder, byteOrder)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Index"/>.
        /// </summary>
        /// <param name="i">Value of the index component.</param>
        /// <param name="a">Value of the alpha component.</param>
        /// <param name="componentOrder">The order of the components.</param>
        /// <param name="byteOrder">The byte order in which atomic values are read.</param>
        public Index(int i, int a, string componentOrder = "IA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            base(new IndexPixelDescriptor(componentOrder, i, a), byteOrder)
        {
        }
    }
}
