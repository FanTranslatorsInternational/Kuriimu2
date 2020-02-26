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
    public abstract class BlockCompressionEncoding<TBlock> : IColorEncoding
    {
        private readonly ByteOrder _byteOrder;

        protected abstract int ColorsInBlock { get; }

        public abstract int BitDepth { get; }

        public abstract string FormatName { get; }

        protected BlockCompressionEncoding(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public IEnumerable<Color> Load(byte[] input, int taskCount)
        {
            var br = new BinaryReaderX(new MemoryStream(input), _byteOrder);

            return ReadBlocks(br).AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .SelectMany(DecodeNextBlock);
        }

        public byte[] Save(IEnumerable<Color> colors, int taskCount)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, _byteOrder);

            var blocks = colors.Batch(ColorsInBlock)
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(taskCount)
                .Select(c => EncodeNextBlock(c.ToArray()));

            foreach (var block in blocks)
                WriteNextBlock(bw, block);

            return ms.ToArray();
        }

        protected abstract TBlock ReadNextBlock(BinaryReaderX br);

        protected abstract void WriteNextBlock(BinaryWriterX bw, TBlock block);

        protected abstract IList<Color> DecodeNextBlock(TBlock block);

        protected abstract TBlock EncodeNextBlock(IList<Color> colors);

        private IEnumerable<TBlock> ReadBlocks(BinaryReaderX br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
                yield return ReadNextBlock(br);
        }
    }
}
