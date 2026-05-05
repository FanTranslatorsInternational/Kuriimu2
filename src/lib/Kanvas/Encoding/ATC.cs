using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding
{
    public class Atc : IColorEncoding
    {
        private readonly AtcFormat _format;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Atc(AtcFormat format)
        {
            _format = format;

            var hasSecondBlock = HasSecondBlock(format);

            BitsPerValue = hasSecondBlock ? 128 : 64;
            BitDepth = hasSecondBlock ? 8 : 4;

            FormatName = format.ToString().Replace("_", " ");
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions options)
        {
            var compressionFormat = GetCompressionFormat();
            var decoder = GetDecoder();

            var blockSize = BitsPerValue / 8;
            return Enumerable.Range(0, input.Length/blockSize).AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(options.TaskCount)
                .SelectMany(x =>
                {
                    var decodedBlock = decoder.DecodeBlock(input.AsSpan(x * blockSize, blockSize), compressionFormat);

                    decodedBlock.TryGetMemory(out var memory);
                    return memory.ToArray().Select(y => new Rgba32( y.r, y.g, y.b, y.a));
                });
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            var compressionFormat = GetCompressionFormat();
            var encoder = GetEncoder(compressionFormat);

            var blockSize = BitsPerValue / 8;
            var widthBlocks = ((options.Size.Width + 3) & ~3) >> 2;
            var heightBlocks = ((options.Size.Height + 3) & ~3) >> 2;
            var buffer = new byte[widthBlocks * heightBlocks * blockSize];

            colors.Chunk(ColorsPerValue).Select((x, i) => (x, i))
                .AsParallel()
                .WithDegreeOfParallelism(options.TaskCount)
                .ForAll(element =>
                {
                    byte[] encodedBlock = encoder.EncodeBlock(element.x.Select(y => new ColorRgba32(y.R, y.G, y.B, y.A)).ToArray());
                    Array.Copy(encodedBlock, 0, buffer, element.i * blockSize, blockSize);
                });

            return buffer;
        }

        private static bool HasSecondBlock(AtcFormat format)
        {
            return format == AtcFormat.Atc_Explicit ||
                   format == AtcFormat.Atc_Interpolated;
        }

        private static BcDecoder GetDecoder()
        {
            return new BcDecoder();
        }

        private static BcEncoder GetEncoder(CompressionFormat compressionFormat)
        {
            return new BcEncoder(compressionFormat);
        }

        private CompressionFormat GetCompressionFormat()
        {
            return _format switch
            {
                AtcFormat.Atc => CompressionFormat.Atc,
                AtcFormat.Atc_Explicit => CompressionFormat.AtcExplicitAlpha,
                AtcFormat.Atc_Interpolated => CompressionFormat.AtcInterpolatedAlpha,
                _ => throw new InvalidOperationException($"Unsupported AtcFormat {_format}.")
            };
        }
    }

    public enum AtcFormat
    {
        Atc,
        Atc_Explicit,
        Atc_Interpolated
    }
}
