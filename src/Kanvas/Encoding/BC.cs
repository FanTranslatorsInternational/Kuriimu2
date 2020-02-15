using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompressions;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    public class BC : BlockCompressionEncoding
    {
        private readonly BcTranscoder _transcoder;
        private readonly bool _hasSecondBlock;

        protected override int ColorsInBlock { get; }

        public override int BitDepth { get; }

        public override int BlockBitDepth { get; }

        public override string FormatName { get; }

        public BC(BcFormat format, ByteOrder byteOrder, int taskCount) : base(byteOrder, taskCount)
        {
            _transcoder = new BcTranscoder(format);
            _hasSecondBlock = HasSecondBlock(format);

            ColorsInBlock = 16;

            BitDepth = _hasSecondBlock ? 8 : 4;
            BlockBitDepth = _hasSecondBlock ? 128 : 64;

            FormatName = format.ToString();
        }

        protected override IEnumerable<Color> DecodeNextBlock(BinaryReaderX br)
        {
            var block1 = br.ReadUInt64();
            var block2 = _hasSecondBlock ? br.ReadUInt64() : ulong.MaxValue;

            return _transcoder.DecodeBlocks(block1, block2);
        }

        protected override void EncodeNextBlock(BinaryWriterX bw, IList<Color> colors)
        {
            var pixelData = _transcoder.EncodeColors(colors);

            bw.Write(pixelData.Block1);
            if (_hasSecondBlock) bw.Write(pixelData.Block2);
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
