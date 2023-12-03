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
        public IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext)
        {
            var compressionFormat = GetCompressionFormat();
            var decoder = GetDecoder();

            var blockSize = BitsPerValue / 8;
            return Enumerable.Range(0, input.Length/blockSize).AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(loadContext.TaskCount)
                .SelectMany(x =>
                {
                    var decodedBlock = decoder.DecodeBlock(input.AsSpan(x * blockSize, blockSize), compressionFormat);

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

        private bool HasSecondBlock(AtcFormat format)
        {
            return format == AtcFormat.Atc_Explicit ||
                   format == AtcFormat.Atc_Interpolated;
        }

        private BcDecoder GetDecoder()
        {
            return new BcDecoder();
        }

        private BcEncoder GetEncoder(CompressionFormat compressionFormat)
        {
            return new BcEncoder(compressionFormat);
        }

        private CompressionFormat GetCompressionFormat()
        {
            switch (_format)
            {
                case AtcFormat.Atc:
                    return CompressionFormat.Atc;

                case AtcFormat.Atc_Explicit:
                    return CompressionFormat.AtcExplicitAlpha;

                case AtcFormat.Atc_Interpolated:
                    return CompressionFormat.AtcInterpolatedAlpha;

                default:
                    throw new InvalidOperationException($"Unsupported AtcFormat {_format}.");
            }
        }
    }

    public enum AtcFormat
    {
        Atc,
        Atc_Explicit,
        Atc_Interpolated
    }
}
