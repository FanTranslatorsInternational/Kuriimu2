using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding
{
    public class Bc : IColorEncoding
    {
        private readonly BcFormat _format;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Bc(BcFormat format)
        {
            _format = format;

            var hasSecondBlock = HasSecondBlock(format);

            BitsPerValue = hasSecondBlock ? 128 : 64;
            BitDepth = hasSecondBlock ? 8 : 4;

            FormatName = format.ToString();
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions options)
        {
            var blockSize = BitsPerValue / 8;

            var compressionFormat = GetCompressionFormat();
            var decoder = GetDecoder();

            return Enumerable.Range(0, input.Length / blockSize).AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(options.TaskCount)
                .SelectMany(x =>
                {
                    var span = input.AsSpan(x * blockSize, blockSize);

                    // Filter out null blocks with error color for BC7 and BC6H
                    if (_format == BcFormat.Bc7 || _format == BcFormat.Bc6H)
                        if (input.Skip(x * blockSize).Take(blockSize).All(b => b == 0))
                            return Enumerable.Repeat((Rgba32)Color.Magenta, blockSize);

                    var decodedBlock = decoder.DecodeBlock(span, compressionFormat);

                    decodedBlock.TryGetMemory(out var memory);
                    return memory.ToArray().Select(y => new Rgba32(y.r, y.g, y.b, y.a));
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

        private bool HasSecondBlock(BcFormat format)
        {
            return format == BcFormat.Bc2 ||
                   format == BcFormat.Bc3 ||
                   format == BcFormat.Bc5 ||
                   format == BcFormat.Bc6H ||
                   format == BcFormat.Bc7 ||
                   format == BcFormat.Ati2AL;
        }

        private BcDecoder GetDecoder()
        {
            var decoder = new BcDecoder();

            switch (_format)
            {
                case BcFormat.Ati1A:
                    decoder.OutputOptions.Bc4Component = ColorComponent.A;
                    break;

                case BcFormat.Ati1L:
                    decoder.OutputOptions.Bc4Component = ColorComponent.Luminance;
                    break;

                case BcFormat.Ati2AL:
                    decoder.OutputOptions.Bc5Component1 = ColorComponent.Luminance;
                    decoder.OutputOptions.Bc5Component2 = ColorComponent.A;
                    break;
            }

            return decoder;
        }

        private BcEncoder GetEncoder(CompressionFormat compressionFormat)
        {
            var encoder = new BcEncoder(compressionFormat);

            switch (_format)
            {
                case BcFormat.Ati1A:
                    encoder.InputOptions.Bc4Component = ColorComponent.A;
                    break;

                case BcFormat.Ati1L:
                    encoder.InputOptions.Bc4Component = ColorComponent.Luminance;
                    break;

                case BcFormat.Ati2AL:
                    encoder.InputOptions.Bc5Component1 = ColorComponent.Luminance;
                    encoder.InputOptions.Bc5Component2 = ColorComponent.A;
                    break;
            }

            return encoder;
        }

        private CompressionFormat GetCompressionFormat()
        {
            switch (_format)
            {
                case BcFormat.Bc1:
                    return CompressionFormat.Bc1;

                case BcFormat.Bc2:
                    return CompressionFormat.Bc2;

                case BcFormat.Bc3:
                    return CompressionFormat.Bc3;

                case BcFormat.Bc4:
                case BcFormat.Ati1A:
                case BcFormat.Ati1L:
                    return CompressionFormat.Bc4;

                case BcFormat.Bc5:
                case BcFormat.Ati2AL:
                    return CompressionFormat.Bc5;

                case BcFormat.Bc6H:
                    return CompressionFormat.Bc6U;

                case BcFormat.Bc7:
                    return CompressionFormat.Bc7;

                default:
                    throw new InvalidOperationException($"Unsupported BcFormat {_format}.");
            }
        }
    }

    /// <summary>
    /// The format identifier for BCs.
    /// </summary>
    /// <remarks>
    /// The WiiU contains non-standardized implementations for BC4 and BC5.<para />
    /// The WiiU implements BC4 with target Alpha or Luminance (RGB channels), instead of Red.<para />
    /// The WiiU implements BC5 with target Alpha/Luminance, instead of Red/Green.
    /// </remarks>
    public enum BcFormat
    {
        Bc1,
        Bc2,
        Bc3,
        Bc4,
        Bc5,
        Bc6H,
        Bc7,

        // WiiU specifications
        Ati1A,
        Ati1L,
        Ati2AL,

        // DXT definitions
        Dxt1 = Bc1,
        Dxt3,
        Dxt5,

        // ATI definitions
        Ati1 = Bc4,
        Ati2
    }
}
