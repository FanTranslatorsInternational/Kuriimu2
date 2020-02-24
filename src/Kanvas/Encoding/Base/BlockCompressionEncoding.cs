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
        private readonly int _taskCount;

        protected abstract int ColorsInBlock { get; }

        public abstract int BitDepth { get; }

        public abstract int BlockBitDepth { get; }

        public abstract string FormatName { get; }

        public bool IsBlockCompression => true;

        protected BlockCompressionEncoding(ByteOrder byteOrder, int taskCount)
        {
            _byteOrder = byteOrder;
            _taskCount = taskCount;
        }

        public IEnumerable<Color> Load(byte[] input)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            var blockByteDepth = BlockBitDepth / 8;
            var blocks = (int)br.BaseStream.Length / blockByteDepth;

            return Enumerable.Range(0, blocks)
                .SelectMany(x => DecodeNextBlock(br));

            // TODO: Fix race conditioning
            //return Enumerable.Range(0, blocks).AsParallel()
            //    .AsOrdered()
            //    .WithDegreeOfParallelism(_taskCount)
            //    .SelectMany(x => DecodeNextBlock(br));
        }

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, _byteOrder))
            {
                colors.Batch(ColorsInBlock).ForEach(colorBatch =>
                    EncodeNextBlock(bw, colorBatch.ToArray()));
                // TODO: Fix race conditioning
                //colors.Batch(ColorsInBlock).AsParallel()
                //    .AsOrdered()
                //    .WithDegreeOfParallelism(_taskCount)
                //    .ForAll(colorBatch => EncodeNextBlock(bw, colorBatch.ToArray()));
            }

            return ms.ToArray();
        }

        protected abstract IEnumerable<Color> DecodeNextBlock(BinaryReaderX br);

        protected abstract void EncodeNextBlock(BinaryWriterX bw, IList<Color> colors);
    }
}
