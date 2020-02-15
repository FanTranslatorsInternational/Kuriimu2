using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.Base;
using Kanvas.Encoding.BlockCompressions;
using Komponent.IO;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ETC1 encoding.
    /// </summary>
    public class ETC1 : BlockCompressionEncoding
    {
        private readonly bool _useAlpha;

        private readonly Etc1Transcoder _transcoder;

        protected override int ColorsInBlock { get; }

        public override int BitDepth { get; }

        public override int BlockBitDepth { get; }

        public override string FormatName { get; }

        public ETC1(bool useAlpha, bool useZOrder, ByteOrder byteOrder, int taskCount) : base(byteOrder, taskCount)
        {
            _useAlpha = useAlpha;
            _transcoder = new Etc1Transcoder(useZOrder);

            ColorsInBlock = 16;

            BitDepth = useAlpha ? 8 : 4;
            BlockBitDepth = useAlpha ? 128 : 64;

            FormatName = "ETC1" + (useAlpha ? "A4" : "");
        }

        protected override IEnumerable<Color> DecodeNextBlock(BinaryReaderX br)
        {
            var alpha = _useAlpha ? br.ReadUInt64() : ulong.MaxValue;
            var colors = br.ReadUInt64();

            return _transcoder.DecodeBlocks(colors, alpha);
        }

        protected override void EncodeNextBlock(BinaryWriterX bw, IList<Color> colors)
        {
            var pixelData = _transcoder.EncodeColors(colors);

            if (_useAlpha) bw.Write(pixelData.Alpha);
            bw.Write(pixelData.Block.GetBlockData());
        }
    }
}
