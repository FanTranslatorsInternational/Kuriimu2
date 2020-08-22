using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompressions.ATC;
using Kanvas.Encoding.BlockCompressions.ATC.Models;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the Atc encoding.
    /// </summary>
    public class Atc : BlockCompressionEncoding<AtcBlockData>
    {
        private readonly AtcTranscoder _transcoder;
        private readonly bool _hasSecondBlock;

        public override int BitDepth { get; }

        public override int BitsPerValue { get; protected set; }

        public override int ColorsPerValue => 16;

        public override string FormatName { get; }

        public Atc(AtcFormat format, ByteOrder byteOrder) : base(byteOrder)
        {
            _transcoder = new AtcTranscoder(format);
            _hasSecondBlock = HasSecondBlock(format);

            BitDepth = BitsPerValue = _hasSecondBlock ? 128 : 64;

            FormatName = format.ToString();
        }

        protected override AtcBlockData ReadNextBlock(BinaryReaderX br)
        {
            var block1 = br.ReadUInt64();
            var block2 = _hasSecondBlock ? br.ReadUInt64() : ulong.MaxValue;

            return new AtcBlockData
            {
                Block1 = block1,
                Block2 = block2
            };
        }

        protected override void WriteNextBlock(BinaryWriterX bw, AtcBlockData block)
        {
            bw.Write(block.Block1);
            if (_hasSecondBlock) bw.Write(block.Block2);
        }

        protected override IList<Color> DecodeNextBlock(AtcBlockData block)
        {
            return _transcoder.DecodeBlocks(block).ToList();
        }

        protected override AtcBlockData EncodeNextBlock(IList<Color> colors)
        {
            return _transcoder.EncodeColors(colors);
        }

        private bool HasSecondBlock(AtcFormat format)
        {
            return format == AtcFormat.ATCA_Exp ||
                   format == AtcFormat.ATCA_Int;
        }
    }
}
