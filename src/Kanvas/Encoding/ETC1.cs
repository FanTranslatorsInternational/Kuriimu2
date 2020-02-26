using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompressions;
using Kanvas.Encoding.BlockCompressions.ETC1.Models;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the Etc1 encoding.
    /// </summary>
    public class Etc1 : BlockCompressionEncoding<Etc1PixelData>
    {
        private readonly bool _useAlpha;
        private readonly Etc1Transcoder _transcoder;

        protected override int ColorsInBlock => 16;

        public override int BitDepth { get; }

        public override string FormatName { get; }

        public Etc1(bool useAlpha, bool useZOrder, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(byteOrder)
        {
            _useAlpha = useAlpha;
            _transcoder = new Etc1Transcoder(useZOrder);

            BitDepth = useAlpha ? 128 : 64;

            FormatName = "ETC1" + (useAlpha ? "A4" : "");
        }

        protected override Etc1PixelData ReadNextBlock(BinaryReaderX br)
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

        protected override void WriteNextBlock(BinaryWriterX bw, Etc1PixelData block)
        {
            if (_useAlpha) bw.Write(block.Alpha);
            bw.Write(block.Block.GetBlockData());
        }

        protected override IList<Color> DecodeNextBlock(Etc1PixelData block)
        {
            return _transcoder.DecodeBlocks(block).ToArray();
        }

        protected override Etc1PixelData EncodeNextBlock(IList<Color> colors)
        {
            return _transcoder.EncodeColors(colors);
        }
    }
}
