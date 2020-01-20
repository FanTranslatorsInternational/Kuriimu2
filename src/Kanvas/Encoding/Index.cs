using Kanvas.Encoding.Models;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the default index-only encoding.
    /// </summary>
    public class Index : PixelIndexEncoding
    {
        /// <summary>
        /// Creates a new instance of <see cref="Index"/>.
        /// </summary>
        /// <param name="indexDepth">Depth of the index component.</param>
        public Index(int indexDepth, int alphaDepth, string componentOrder = "IA", ByteOrder byteOrder = ByteOrder.LittleEndian) :
            base(new IndexPixelDescriptor(componentOrder, indexDepth, alphaDepth), byteOrder)
        {
        }
    }
}
