using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using Kanvas.MoreEnumerable;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;
using Microsoft.Toolkit.HighPerformance.Extensions;

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
        public IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext)
        {
            var blockSize = BitsPerValue / 8;

            var compressionFormat = GetCompressionFormat();
            var decoder = GetDecoder();

            return Enumerable.Range(0, input.Length / blockSize).AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .SelectMany(x =>
                {
                    var span = input.AsSpan(x * blockSize, blockSize);

                    // Filter out null blocks with error color for BC7 and BC6H
                    if (_format == BcFormat.Bc7 || _format == BcFormat.Bc6H)
                        if (input.Skip(x * blockSize).Take(blockSize).All(b => b == 0))
                            return Enumerable.Repeat(Color.Magenta, blockSize);

                    var decodedBlock = decoder.DecodeBlock(span, compressionFormat);

                    decodedBlock.TryGetMemory(out var memory);
                    return memory.ToArray().Select(y => Color.FromArgb(y.a, y.r, y.g, y.b));
                });
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            var compressionFormat = GetCompressionFormat();
            var encoder = GetEncoder(compressionFormat);

            var blockSize = BitsPerValue / 8;
            var widthBlocks = ((saveContext.Size.Width + 3) & ~3) >> 2;
            var heightBlocks = ((saveContext.Size.Height + 3) & ~3) >> 2;
            var buffer = new byte[widthBlocks * heightBlocks * blockSize];

            colors.Chunk(ColorsPerValue).Select((x, i) => (x, i))
                .AsParallel()
                .WithDegreeOfParallelism(saveContext.TaskCount)
                .ForAll(element =>
                {
                    var encodedBlock = encoder.EncodeBlock(element.x.Select(y => new ColorRgba32(y.R, y.G, y.B, y.A)).ToArray());
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
