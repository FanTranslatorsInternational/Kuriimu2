using Kanvas.Encoding.BlockCompression.Etc1.Helper;
using Kanvas.Encoding.BlockCompression.Etc1.Models;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.BlockCompression.Etc1
{
    internal class Etc1Transcoder(bool zOrdered)
    {
        public IEnumerable<Rgba32> DecodeBlocks(Etc1PixelData data)
        {
            var basec0 = data.Block.Color0.Scale(data.Block.ColorDepth);
            var basec1 = data.Block.Color1.Scale(data.Block.ColorDepth);

            var flipbitmask = data.Block.FlipBit ? 2 : 8;
            foreach (var i in zOrdered ? Constants.ZOrder : Constants.NormalOrder)
            {
                var basec = (i & flipbitmask) == 0 ? basec0 : basec1;
                var mod = Constants.Modifiers[(i & flipbitmask) == 0 ? data.Block.Table0 : data.Block.Table1];
                var c = basec + mod[data.Block[i]];

                yield return new Rgba32(c.R, c.G, c.B, (byte)((data.Alpha >> 4 * i) % 16 * 17));
            }
        }

        public Etc1PixelData EncodeColors(IList<Rgba32> colorBatch)
        {
            var colorsWindows = Enumerable.Range(0, 16).Select(j => zOrdered ?
                colorBatch[Constants.ZOrder[Constants.ZOrder[Constants.ZOrder[j]]]] :
                colorBatch[Constants.NormalOrder[j]]).ToArray();

            var alpha = colorsWindows.Reverse().Aggregate(0ul, (a, b) => a * 16 | (byte)(b.A / 16));
            var colors = colorsWindows.Select(c2 => new Rgb(c2.R, c2.G, c2.B)).ToList();

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

        private static Block PackSolidColor(Rgb c)
        {
            return (from i in Enumerable.Range(0, 64)
                    let r = Constants.StaticColorLookup[i * 256 + c.R]
                    let g = Constants.StaticColorLookup[i * 256 + c.G]
                    let b = Constants.StaticColorLookup[i * 256 + c.B]
                    orderby ErrorRgb(r >> 8, g >> 8, b >> 8)
                    let soln = new Solution
                    {
                        BlockColor = new Rgb(r, g, b),
                        IntenTable = Constants.Modifiers[i >> 2 & 7],
                        SelectorMsb = (i & 2) == 2 ? 0xFF : 0,
                        SelectorLsb = (i & 1) == 1 ? 0xFF : 0
                    }
                    select new SolutionSet(false, (i & 32) == 32, soln, soln).ToBlock())
                .First();
        }

        private static int ErrorRgb(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception
    }
}
