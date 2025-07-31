using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompression.Etc1;
using Kanvas.Encoding.BlockCompression.Etc1.Models;
using Komponent.Contract.Enums;
using Komponent.IO;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the Etc1 encoding.
    /// </summary>
    public class Etc1 : BlockCompressionEncoding<Etc1PixelData>
    {
        private readonly bool _useAlpha;
        private readonly Etc1Transcoder _transcoder;

        /// <inheritdoc cref="BitDepth"/>
        public override int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public override int BitsPerValue { get; protected set; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public override int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public override string FormatName { get; }

        public Etc1(bool useAlpha, bool useZOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) : 
            base(byteOrder)
        {
            _useAlpha = useAlpha;
            _transcoder = new Etc1Transcoder(useZOrder);

            BitsPerValue = useAlpha ? 128 : 64;
            BitDepth = useAlpha ? 8 : 4;

            FormatName = "ETC1" + (useAlpha ? "A4" : "");
            if (useZOrder)
                FormatName += " (Z-Order)";
        }

        protected override Etc1PixelData ReadBlock(BinaryReaderX br)
        {
            var alpha = _useAlpha ? br.ReadUInt64() : ulong.MaxValue;
            var colors = br.ReadUInt64();

            return new Etc1PixelData
            {
                Alpha = alpha,
                Block = new Block
                {
                    LSB = (ushort)(colors & 0xFFFF),
                    MSB = (ushort)((colors >> 16) & 0xFFFF),
                    Flags = (byte)((colors >> 32) & 0xFF),
                    B = (byte)((colors >> 40) & 0xFF),
                    G = (byte)((colors >> 48) & 0xFF),
                    R = (byte)((colors >> 56) & 0xFF)
                }
            };
        }

        protected override void WriteBlock(BinaryWriterX bw, Etc1PixelData block)
        {
            if (_useAlpha) bw.Write(block.Alpha);
            bw.Write(block.Block.GetBlockData());
        }

        protected override IList<Rgba32> DecodeBlock(Etc1PixelData block)
        {
            return _transcoder.DecodeBlocks(block).ToArray();
        }

        protected override Etc1PixelData EncodeBlock(IList<Rgba32> colors)
        {
            return _transcoder.EncodeColors(colors);
        }
    }
}
