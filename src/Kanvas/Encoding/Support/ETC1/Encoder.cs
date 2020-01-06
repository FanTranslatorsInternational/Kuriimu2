using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding.Support.ETC1.Helper;
using Kanvas.Encoding.Support.ETC1.Models;

namespace Kanvas.Encoding.Support.ETC1
{
    internal class Encoder
    {
        private readonly bool _zOrdered;
        private readonly List<Color> _queue;

        private static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));
        private static int ErrorRGB(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception

        private static readonly int[] _solidColorLookup =
            (from limit in new[] { 16, 32 }
             from inten in Constants.Modifiers
             from selector in inten
             from color in Enumerable.Range(0, 256)
             select Enumerable.Range(0, limit).Min(packed_c =>
             {
                 int c = (limit == 32) ? (packed_c << 3) | (packed_c >> 2) : packed_c * 17;
                 return (Math.Abs(Clamp(c + selector) - color) << 8) | packed_c;
             })).ToArray();

        public Encoder(bool zOrdered)
        {
            _zOrdered = zOrdered;
            _queue = new List<Color>();
        }

        public static Block PackSolidColor(RGB c)
        {
            return (from i in Enumerable.Range(0, 64)
                    let r = _solidColorLookup[i * 256 + c.R]
                    let g = _solidColorLookup[i * 256 + c.G]
                    let b = _solidColorLookup[i * 256 + c.B]
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

        public void Set(Color c, Action<Etc1PixelData> func)
        {
            _queue.Add(c);
            if (_queue.Count == 16)
            {
                var colorsWindows = Enumerable.Range(0, 16).Select(j => _zOrdered ?
                    _queue[Constants.ZOrder[Constants.ZOrder[Constants.ZOrder[j]]]] :
                    _queue[Constants.NormalOrder[j]]).ToArray();

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

                func(new Etc1PixelData { Alpha = alpha, Block = block });
                _queue.Clear();
            }
        }
    }
}
