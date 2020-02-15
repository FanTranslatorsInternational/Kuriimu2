using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ETC1;
using Kanvas.Encoding.BlockCompressions.ETC1.Helper;
using Kanvas.Encoding.BlockCompressions.ETC1.Models;

namespace Kanvas.Encoding.BlockCompressions
{
    class Etc1Transcoder
    {
        private readonly bool _zOrdered;

        public Etc1Transcoder(bool zOrdered)
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

        public Etc1PixelData EncodeColors(IList<Color> colorBatch)
        {
            var colorsWindows = Enumerable.Range(0, 16).Select(j => _zOrdered ?
                colorBatch[Constants.ZOrder[Constants.ZOrder[Constants.ZOrder[j]]]] :
                colorBatch[Constants.NormalOrder[j]]).ToArray();

            var alpha = colorsWindows.Reverse().Aggregate(0ul, (a, b) => (a * 16) | (byte)(b.A / 16));
            var colors = colorsWindows.Select(c2 => new RGB(c2.R, c2.G, c2.B)).ToList();

            Block block;
            // special case 1: this block has all 16 pixels exactly the same color
            if (colors.All(color => color == colors[0]))
            {
                block = PackSolidColor(colors[0]);
            }
            // special case 2: this block was previously etc1-compressed
            else if (!Optimizer.RepackEtc1CompressedBlock(colors, out block))
            {
                block = Optimizer.Encode(colors);
            }

            return new Etc1PixelData { Alpha = alpha, Block = block };
        }

        private Block PackSolidColor(RGB c)
        {
            return (from i in Enumerable.Range(0, 64)
                    let r = Constants.StaticColorLookup[i * 256 + c.R]
                    let g = Constants.StaticColorLookup[i * 256 + c.G]
                    let b = Constants.StaticColorLookup[i * 256 + c.B]
                    orderby ErrorRGB(r >> 8, g >> 8, b >> 8)
                    let soln = new Solution
                    {
                        BlockColor = new RGB(r, g, b),
                        IntenTable = Constants.Modifiers[(i >> 2) & 7],
                        SelectorMSB = (i & 2) == 2 ? 0xFF : 0,
                        SelectorLSB = (i & 1) == 1 ? 0xFF : 0
                    }
                    select new SolutionSet(false, (i & 32) == 32, soln, soln).ToBlock())
                .First();
        }

        private static int ErrorRGB(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception
    }
}
