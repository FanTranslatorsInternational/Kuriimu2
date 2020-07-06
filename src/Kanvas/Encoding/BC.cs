using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompressions;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    public class Bc : BlockCompressionEncoding<BcPixelData>
    {
        private readonly BcTranscoder _transcoder;
        private readonly bool _hasSecondBlock;

        public override int BitsPerValue { get; protected set; }

        public override int ColorsPerValue => 16;

        public override int BitDepth { get; }

        public override string FormatName { get; }

        public Bc(BcFormat format, ByteOrder byteOrder = ByteOrder.LittleEndian) : base(byteOrder)
        {
            _transcoder = new BcTranscoder(format);
            _hasSecondBlock = HasSecondBlock(format);

            BitsPerValue = _hasSecondBlock ? 128 : 64;
            BitDepth = _hasSecondBlock ? 8 : 4;

            FormatName = format.ToString();
        }

        protected override BcPixelData ReadNextBlock(BinaryReaderX br)
        {
            var block1 = br.ReadUInt64();
            var block2 = _hasSecondBlock ? br.ReadUInt64() : ulong.MaxValue;

            return new BcPixelData
            {
                Block1 = block1,
                Block2 = block2
            };
        }

        protected override void WriteNextBlock(BinaryWriterX bw, BcPixelData block)
        {
            bw.Write(block.Block1);
            if (_hasSecondBlock) bw.Write(block.Block2);
        }

        protected override IList<Color> DecodeNextBlock(BcPixelData block)
        {
            return _transcoder.DecodeBlocks(block).ToList();
        }

        protected override BcPixelData EncodeNextBlock(IList<Color> colors)
        {
            return _transcoder.EncodeColors(colors);
        }

        private bool HasSecondBlock(BcFormat format)
        {
            return format == BcFormat.BC2 ||
                   format == BcFormat.BC3 ||
                   format == BcFormat.BC5 ||
                   format == BcFormat.ATI2_WiiU;
        }
    }
}
