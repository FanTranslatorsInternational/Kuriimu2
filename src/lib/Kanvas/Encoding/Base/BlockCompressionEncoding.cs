using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Komponent.IO;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.Base
{
    public abstract class BlockCompressionEncoding<TBlock>(ByteOrder byteOrder) : IColorEncoding
    {
        /// <inheritdoc cref="BitDepth"/>
        public abstract int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public abstract int BitsPerValue { get; protected set; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public abstract int ColorsPerValue { get; }

        /// <inheritdoc cref="FormatName"/>
        public abstract string FormatName { get; }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions loadContext)
        {
            IEnumerable<TBlock> blocks = ReadBlocks(input);

            return blocks.AsParallel().AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .SelectMany(DecodeBlock);
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions saveContext)
        {
            IEnumerable<TBlock> blocks = colors.Chunk(ColorsPerValue)
                .AsParallel().AsOrdered()
                .WithDegreeOfParallelism(saveContext.TaskCount)
                .Select(c => EncodeBlock([.. c]));

            return WriteBlocks(blocks);
        }

        protected abstract TBlock ReadBlock(BinaryReaderX br);

        protected abstract void WriteBlock(BinaryWriterX bw, TBlock block);

        protected abstract IList<Rgba32> DecodeBlock(TBlock block);

        protected abstract TBlock EncodeBlock(IList<Rgba32> colors);

        private IEnumerable<TBlock> ReadBlocks(byte[] input)
        {
            var reader = new BinaryReaderX(new MemoryStream(input), byteOrder);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
                yield return ReadBlock(reader);
        }

        private byte[] WriteBlocks(IEnumerable<TBlock> blocks)
        {
            var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms, byteOrder);

            foreach (TBlock block in blocks)
                WriteBlock(bw, block);

            return ms.ToArray();
        }
    }
}
