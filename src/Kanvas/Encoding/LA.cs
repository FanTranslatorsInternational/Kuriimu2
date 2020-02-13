using Kanvas.Encoding.Base;
using Kanvas.Encoding.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the LA encoding.
    /// </summary>
    public class LA : PixelEncoding
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LA"/>.
        /// </summary>
        /// <param name="l">Value of the luminence component.</param>
        /// <param name="a">Value of the alpha component.</param>
        public LA(int l, int a, string componentOrder = "LA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            base(new LaPixelDescriptor(componentOrder, l, a), byteOrder)
        {
        }
    }
}
