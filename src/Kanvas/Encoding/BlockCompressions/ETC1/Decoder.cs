using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.BlockCompressions.ETC1.Models;

namespace Kanvas.Encoding.BlockCompressions.ETC1
{
    internal class Decoder
    {
        private readonly bool _zOrdered;

        public Decoder(bool zOrdered)
        {
            _zOrdered = zOrdered;
        }

        public IEnumerable<Color> DecodeBlocks(ulong colorData, ulong alphaData)
        {
            var etc1Block = new Block
            {
                LSB = (ushort)(colorData & 0xFFFF),
                MSB = (ushort)((colorData >> 16) & 0xFFFF),
                Flags = (byte)((colorData >> 32) & 0xFF),
                B = (byte)((colorData >> 40) & 0xFF),
                G = (byte)((colorData >> 48) & 0xFF),
                R = (byte)((colorData >> 56) & 0xFF)
            };

            var basec0 = etc1Block.Color0.Scale(etc1Block.ColorDepth);
            var basec1 = etc1Block.Color1.Scale(etc1Block.ColorDepth);

            int flipbitmask = etc1Block.FlipBit ? 2 : 8;
            foreach (var i in _zOrdered ? Constants.ZOrder : Constants.NormalOrder)
            {
                var basec = (i & flipbitmask) == 0 ? basec0 : basec1;
                var mod = Constants.Modifiers[(i & flipbitmask) == 0 ? etc1Block.Table0 : etc1Block.Table1];
                var c = basec + mod[etc1Block[i]];

                yield return Color.FromArgb((int)((alphaData >> (4 * i)) % 16 * 17), c.R, c.G, c.B);
            }
        }
    }
}
