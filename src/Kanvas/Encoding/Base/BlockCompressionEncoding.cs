using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.MoreEnumerable;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding.Base
{
    public abstract class BlockCompressionEncoding : IColorEncoding
    {
        private readonly ByteOrder _byteOrder;

        protected abstract int ColorsInBlock { get; }

        public abstract int BitDepth { get; }

        public abstract int BlockBitDepth { get; }

        public abstract string FormatName { get; }

        public bool IsBlockCompression => true;

        protected BlockCompressionEncoding(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] input)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                foreach (var color in DecodeNextBlock(br))
                    yield return color;
            }
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, _byteOrder))
            {
                foreach (var colorBatch in colors.Batch(ColorsInBlock))
                    EncodeNextBlock(bw, colorBatch.ToArray());
            }

            return ms.ToArray();
        }

        protected abstract IEnumerable<Color> DecodeNextBlock(BinaryReaderX br);

        protected abstract void EncodeNextBlock(BinaryWriterX bw, IList<Color> colors);
    }
}
