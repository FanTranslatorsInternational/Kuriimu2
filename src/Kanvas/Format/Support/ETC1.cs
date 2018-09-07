using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kontract.Image.Support
{
    public class ETC1
    {
        static readonly int[] zOrder = { 0, 4, 1, 5, 8, 12, 9, 13, 2, 6, 3, 7, 10, 14, 11, 15 };
        static readonly int[] normalOrder = { 0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 };

        static int[][] modifiers =
        {
            new[] { 2, 8, -2, -8 },
            new[] { 5, 17, -5, -17 },
            new[] { 9, 29, -9, -29 },
            new[] { 13, 42, -13, -42 },
            new[] { 18, 60, -18, -60 },
            new[] { 24, 80, -24, -80 },
            new[] { 33, 106, -33, -106 },
            new[] { 47, 183, -47, -183 }
        };

        [DebuggerDisplay("{R},{G},{B}")]
        public struct RGB
        {
            public byte R, G, B, padding; // padding for speed reasons

            public RGB(int r, int g, int b)
            {
                R = (byte)r;
                G = (byte)g;
                B = (byte)b;
                padding = 0;
            }

            public static RGB operator +(RGB c, int mod) => new RGB(Clamp(c.R + mod), Clamp(c.G + mod), Clamp(c.B + mod));
            public static int operator -(RGB c1, RGB c2) => ErrorRGB(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B);
            public static RGB Average(RGB[] src) => new RGB((int)src.Average(c => c.R), (int)src.Average(c => c.G), (int)src.Average(c => c.B));
            public RGB Scale(int limit) => limit == 16 ? new RGB(R * 17, G * 17, B * 17) : new RGB((R << 3) | (R >> 2), (G << 3) | (G >> 2), (B << 3) | (B >> 2));
            public RGB Unscale(int limit) => new RGB(R * limit / 256, G * limit / 256, B * limit / 256);

            public override int GetHashCode() => R | (G << 8) | (B << 16);
            public override bool Equals(object obj) => obj != null && GetHashCode() == obj.GetHashCode();
            public static bool operator ==(RGB c1, RGB c2) => c1.Equals(c2);
            public static bool operator !=(RGB c1, RGB c2) => !c1.Equals(c2);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Block
        {
            public ushort LSB;
            public ushort MSB;
            public byte flags;
            public byte B;
            public byte G;
            public byte R;

            public bool FlipBit
            {
                get { return (flags & 1) == 1; }
                set { flags = (byte)((flags & ~1) | (value ? 1 : 0)); }
            }
            public bool DiffBit
            {
                get { return (flags & 2) == 2; }
                set { flags = (byte)((flags & ~2) | (value ? 2 : 0)); }
            }
            public int ColorDepth => DiffBit ? 32 : 16;
            public int Table0
            {
                get { return (flags >> 5) & 7; }
                set { flags = (byte)((flags & ~(7 << 5)) | (value << 5)); }
            }
            public int Table1
            {
                get { return (flags >> 2) & 7; }
                set { flags = (byte)((flags & ~(7 << 2)) | (value << 2)); }
            }
            public int this[int i] => (MSB >> i) % 2 * 2 + (LSB >> i) % 2;

            public RGB Color0 => new RGB(R * ColorDepth / 256, G * ColorDepth / 256, B * ColorDepth / 256);

            public RGB Color1
            {
                get
                {
                    if (!DiffBit) return new RGB(R % 16, G % 16, B % 16);
                    var c0 = Color0;
                    int rd = Sign3(R % 8), gd = Sign3(G % 8), bd = Sign3(B % 8);
                    return new RGB(c0.R + rd, c0.G + gd, c0.B + bd);
                }
            }
        }

        public struct PixelData
        {
            public ulong Alpha { get; set; }
            public Block Block { get; set; }
        }

        class SolutionSet
        {
            const int MAX_ERROR = 99999999;

            bool flip;
            bool diff;
            Solution soln0;
            Solution soln1;

            public int TotalError => soln0.error + soln1.error;

            public SolutionSet()
            {
                soln1 = soln0 = new Solution { error = MAX_ERROR };
            }

            public SolutionSet(bool flip, bool diff, Solution soln0, Solution soln1)
            {
                this.flip = flip;
                this.diff = diff;
                this.soln0 = soln0;
                this.soln1 = soln1;
            }

            public Block ToBlock()
            {
                var blk = new Block
                {
                    DiffBit = diff,
                    FlipBit = flip,
                    Table0 = Array.IndexOf(modifiers, soln0.intenTable),
                    Table1 = Array.IndexOf(modifiers, soln1.intenTable)
                };

                if (blk.FlipBit)
                {
                    int m0 = soln0.selectorMSB, m1 = soln1.selectorMSB;
                    m0 = (m0 & 0xC0) * 64 + (m0 & 0x30) * 16 + (m0 & 0xC) * 4 + (m0 & 0x3);
                    m1 = (m1 & 0xC0) * 64 + (m1 & 0x30) * 16 + (m1 & 0xC) * 4 + (m1 & 0x3);
                    blk.MSB = (ushort)(m0 + 4 * m1);
                    int l0 = soln0.selectorLSB, l1 = soln1.selectorLSB;
                    l0 = (l0 & 0xC0) * 64 + (l0 & 0x30) * 16 + (l0 & 0xC) * 4 + (l0 & 0x3);
                    l1 = (l1 & 0xC0) * 64 + (l1 & 0x30) * 16 + (l1 & 0xC) * 4 + (l1 & 0x3);
                    blk.LSB = (ushort)(l0 + 4 * l1);
                }
                else
                {
                    blk.MSB = (ushort)(soln0.selectorMSB + 256 * soln1.selectorMSB);
                    blk.LSB = (ushort)(soln0.selectorLSB + 256 * soln1.selectorLSB);
                }

                if (blk.DiffBit)
                {
                    int rdiff = (soln1.blockColor.R - soln0.blockColor.R + 8) % 8;
                    int gdiff = (soln1.blockColor.G - soln0.blockColor.G + 8) % 8;
                    int bdiff = (soln1.blockColor.B - soln0.blockColor.B + 8) % 8;
                    blk.R = (byte)(soln0.blockColor.R * 8 + rdiff);
                    blk.G = (byte)(soln0.blockColor.G * 8 + gdiff);
                    blk.B = (byte)(soln0.blockColor.B * 8 + bdiff);
                }
                else
                {
                    blk.R = (byte)(soln0.blockColor.R * 16 + soln1.blockColor.R);
                    blk.G = (byte)(soln0.blockColor.G * 16 + soln1.blockColor.G);
                    blk.B = (byte)(soln0.blockColor.B * 16 + soln1.blockColor.B);
                }

                return blk;
            }
        }

        class Solution
        {
            public int error;
            public RGB blockColor;
            public int[] intenTable;
            public int selectorMSB;
            public int selectorLSB;
        }

        static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));
        static int Sign3(int n) => (n + 4) % 8 - 4;
        static int ErrorRGB(int r, int g, int b) => 2 * r * r + 4 * g * g + 3 * b * b; // human perception

        public class Decoder
        {
            Queue<Color> queue = new Queue<Color>();

            bool _zOrdered;

            public Decoder(bool zOrdered)
            {
                this._zOrdered = zOrdered;
            }

            public Color Get(Func<PixelData> func)
            {
                if (!queue.Any())
                {
                    var data = func();
                    var basec0 = data.Block.Color0.Scale(data.Block.ColorDepth);
                    var basec1 = data.Block.Color1.Scale(data.Block.ColorDepth);

                    int flipbitmask = data.Block.FlipBit ? 2 : 8;
                    foreach (int i in (_zOrdered) ? zOrder : normalOrder)
                    {
                        var basec = (i & flipbitmask) == 0 ? basec0 : basec1;
                        var mod = modifiers[(i & flipbitmask) == 0 ? data.Block.Table0 : data.Block.Table1];
                        var c = basec + mod[data.Block[i]];
                        queue.Enqueue(Color.FromArgb((int)((data.Alpha >> (4 * i)) % 16 * 17), c.R, c.G, c.B));
                    }
                }
                return queue.Dequeue();
            }
        }

        public class Encoder
        {
            static int[] solidColorLookup = (from limit in new[] { 16, 32 }
                                             from inten in modifiers
                                             from selector in inten
                                             from color in Enumerable.Range(0, 256)
                                             select Enumerable.Range(0, limit).Min(packed_c =>
                                             {
                                                 int c = (limit == 32) ? (packed_c << 3) | (packed_c >> 2) : packed_c * 17;
                                                 return (Math.Abs(Clamp(c + selector) - color) << 8) | packed_c;
                                             })).ToArray();

            List<Color> queue = new List<Color>();

            bool _zOrdered;

            public Encoder(bool zOrdered)
            {
                _zOrdered = zOrdered;
            }

            public static Block PackSolidColor(RGB c)
            {
                return (from i in Enumerable.Range(0, 64)
                        let r = solidColorLookup[i * 256 + c.R]
                        let g = solidColorLookup[i * 256 + c.G]
                        let b = solidColorLookup[i * 256 + c.B]
                        orderby ErrorRGB(r >> 8, g >> 8, b >> 8)
                        let soln = new Solution
                        {
                            blockColor = new RGB(r, g, b),
                            intenTable = modifiers[(i >> 2) & 7],
                            selectorMSB = (i & 2) == 2 ? 0xFF : 0,
                            selectorLSB = (i & 1) == 1 ? 0xFF : 0
                        }
                        select new SolutionSet(false, (i & 32) == 32, soln, soln).ToBlock())
                        .First();
            }

            public void Set(Color c, Action<PixelData> func)
            {
                queue.Add(c);
                if (queue.Count == 16)
                {
                    var colorsWindows = Enumerable.Range(0, 16).Select(j => (_zOrdered) ? queue[zOrder[zOrder[zOrder[j]]]] : queue[normalOrder[j]]); // invert order3ds
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

                    func(new PixelData { Alpha = alpha, Block = block });
                    queue.Clear();
                }
            }
        }

        // Loosely based on rg_etc1
        class Optimizer
        {
            RGB[] pixels;
            public RGB baseColor;
            int limit;
            Solution best_soln;

            Optimizer(RGB[] pixels, int limit, int error)
            {
                this.pixels = pixels;
                this.limit = limit;
                baseColor = RGB.Average(pixels).Unscale(limit);
                best_soln = new Solution { error = error };
            }

            bool ComputeDeltas(params int[] deltas)
            {
                return TestUnscaledColors(from zd in deltas
                                          let z = zd + baseColor.B
                                          where z >= 0 && z < limit
                                          from yd in deltas
                                          let y = yd + baseColor.G
                                          where y >= 0 && y < limit
                                          from xd in deltas
                                          let x = xd + baseColor.R
                                          where x >= 0 && x < limit
                                          select new RGB(x, y, z));
            }

            IEnumerable<Solution> FindExactMatches(IEnumerable<RGB> colors, int[] intenTable)
            {
                foreach (var c in colors)
                {
                    best_soln.error = 1;
                    if (EvaluateSolution(c, intenTable))
                        yield return best_soln;
                }
            }

            bool TestUnscaledColors(IEnumerable<RGB> colors)
            {
                bool success = false;
                foreach (var c in colors)
                {
                    foreach (var t in modifiers)
                    {
                        if (EvaluateSolution(c, t))
                        {
                            success = true;
                            if (best_soln.error == 0) return true;
                        }
                    }
                }
                return success;
            }

            bool EvaluateSolution(RGB c, int[] intenTable)
            {
                var soln = new Solution { blockColor = c, intenTable = intenTable };
                var newTable = new RGB[4];
                var scaledColor = c.Scale(limit);
                for (int i = 0; i < 4; i++)
                    newTable[i] = scaledColor + intenTable[i];

                for (int i = 0; i < 8; i++)
                {
                    int best_j = 0, best_error = int.MaxValue;
                    for (int j = 0; j < 4; j++)
                    {
                        int error = pixels[i] - newTable[j];
                        if (error < best_error)
                        {
                            best_error = error;
                            best_j = j;
                        }
                    }
                    soln.error += best_error;
                    if (soln.error >= best_soln.error) return false;
                    soln.selectorMSB |= (byte)(best_j / 2 << i);
                    soln.selectorLSB |= (byte)(best_j % 2 << i);
                }
                best_soln = soln;
                return true;
            }

            #region Pre-computed lookup table for recompressing etc1
            static bool[][] lookup16 = new bool[8][];
            static bool[][] lookup32 = new bool[8][];
            static byte[][][] lookup16big = new byte[8][][];
            static byte[][][] lookup32big = new byte[8][][];
            static Optimizer()
            {
                for (int i = 0; i < 8; i++)
                {
                    lookup16[i] = new bool[256];
                    lookup32[i] = new bool[256];
                    lookup16big[i] = new byte[16][];
                    lookup32big[i] = new byte[32][];
                    for (int j = 0; j < 16; j++)
                    {
                        lookup16big[i][j] = modifiers[i].Select(mod => (byte)Clamp(j * 17 + mod)).Distinct().ToArray();
                        foreach (var k in lookup16big[i][j]) lookup16[i][k] = true;
                    }
                    for (int j = 0; j < 32; j++)
                    {
                        lookup32big[i][j] = modifiers[i].Select(mod => (byte)Clamp(j * 8 + j / 4 + mod)).Distinct().ToArray();
                        foreach (var k in lookup32big[i][j]) lookup32[i][k] = true;
                    }
                }
            }
            #endregion

            // This is currently still very brute-forcey. Can be improved in the future
            public static bool RepackEtc1CompressedBlock(List<RGB> colors, out Block block)
            {
                foreach (var flip in new[] { false, true })
                {
                    var allpixels0 = colors.Where((c, j) => (j / (flip ? 2 : 8)) % 2 == 0).ToArray();
                    var pixels0 = allpixels0.Distinct().ToArray();
                    if (pixels0.Length > 4) continue;

                    var allpixels1 = colors.Where((c, j) => (j / (flip ? 2 : 8)) % 2 == 1).ToArray();
                    var pixels1 = allpixels1.Distinct().ToArray();
                    if (pixels1.Length > 4) continue;

                    foreach (var diff in new[] { false, true })
                    {
                        if (!diff)
                        {
                            var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => lookup16[i][c.R] && lookup16[i][c.G] && lookup16[i][c.B])).ToList();
                            if (!tables0.Any()) continue;
                            var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => lookup16[i][c.R] && lookup16[i][c.G] && lookup16[i][c.B])).ToList();
                            if (!tables1.Any()) continue;

                            var opt0 = new Optimizer(allpixels0, 16, 1);
                            Solution soln0 = null;
                            foreach (var ti in tables0)
                            {
                                var rs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => lookup16big[ti][a].Contains(c.R))).ToArray();
                                var gs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => lookup16big[ti][a].Contains(c.G))).ToArray();
                                var bs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => lookup16big[ti][a].Contains(c.B))).ToArray();
                                soln0 = opt0.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), modifiers[ti]).FirstOrDefault();
                                if (soln0 != null) break;
                            }
                            if (soln0 == null) continue;

                            var opt1 = new Optimizer(allpixels1, 16, 1);
                            foreach (var ti in tables1)
                            {
                                var rs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => lookup16big[ti][a].Contains(c.R))).ToArray();
                                var gs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => lookup16big[ti][a].Contains(c.G))).ToArray();
                                var bs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => lookup16big[ti][a].Contains(c.B))).ToArray();
                                var soln1 = opt1.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), modifiers[ti]).FirstOrDefault();
                                if (soln1 != null)
                                {
                                    block = new SolutionSet(flip, diff, soln0, soln1).ToBlock();
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => lookup32[i][c.R] && lookup32[i][c.G] && lookup32[i][c.B])).ToList();
                            if (!tables0.Any()) continue;
                            var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => lookup32[i][c.R] && lookup32[i][c.G] && lookup32[i][c.B])).ToList();
                            if (!tables1.Any()) continue;

                            var opt0 = new Optimizer(allpixels0, 32, 1);
                            var solns0 = new List<Solution>();
                            foreach (var ti in tables0)
                            {
                                var rs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => lookup32big[ti][a].Contains(c.R))).ToArray();
                                var gs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => lookup32big[ti][a].Contains(c.G))).ToArray();
                                var bs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => lookup32big[ti][a].Contains(c.B))).ToArray();
                                solns0.AddRange(opt0.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), modifiers[ti]));
                            }
                            if (!solns0.Any()) continue;

                            var opt1 = new Optimizer(allpixels1, 32, 1);
                            foreach (var ti in tables1)
                            {
                                var rs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => lookup32big[ti][a].Contains(c.R))).ToArray();
                                var gs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => lookup32big[ti][a].Contains(c.G))).ToArray();
                                var bs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => lookup32big[ti][a].Contains(c.B))).ToArray();
                                foreach (var soln0 in solns0)
                                {
                                    var q = (from r in rs
                                             let dr = r - soln0.blockColor.R
                                             where dr >= -4 && dr < 4
                                             from g in gs
                                             let dg = g - soln0.blockColor.G
                                             where dg >= -4 && dg < 4
                                             from b in bs
                                             let db = b - soln0.blockColor.B
                                             where db >= -4 && db < 4
                                             select new RGB(r, g, b));
                                    var soln1 = opt1.FindExactMatches(q, modifiers[ti]).FirstOrDefault();
                                    if (soln1 != null)
                                    {
                                        block = new SolutionSet(flip, diff, soln0, soln1).ToBlock();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                block = default(Block);
                return false;
            }

            public static Block Encode(List<RGB> colors)
            {
                // regular case: just try our best to compress and minimise error
                var bestsolns = new SolutionSet();
                foreach (var flip in new[] { false, true })
                {
                    var pixels = new[] { 0, 1 }.Select(i => colors.Where((c, j) => (j / (flip ? 2 : 8)) % 2 == i).ToArray()).ToArray();
                    foreach (var diff in new[] { false, true }) // let's again just assume no diff
                    {
                        var solns = new Solution[2];
                        int limit = diff ? 32 : 16;
                        int i;
                        for (i = 0; i < 2; i++)
                        {
                            var errorThreshold = bestsolns.TotalError;
                            if (i == 1) errorThreshold -= solns[0].error;
                            var opt = new Optimizer(pixels[i], limit, errorThreshold);
                            if (i == 1 && diff)
                            {
                                opt.baseColor = solns[0].blockColor;
                                if (!opt.ComputeDeltas(-4, -3, -2, -1, 0, 1, 2, 3)) break;
                            }
                            else
                            {
                                if (!opt.ComputeDeltas(-4, -3, -2, -1, 0, 1, 2, 3, 4)) break;
                                // TODO: Fix fairly arbitrary/unrefined thresholds that control how far away to scan for potentially better solutions.
                                if (opt.best_soln.error > 9000)
                                {
                                    if (opt.best_soln.error > 18000)
                                        opt.ComputeDeltas(-8, -7, -6, -5, 5, 6, 7, 8);
                                    else
                                        opt.ComputeDeltas(-5, 5);
                                }
                            }
                            if (opt.best_soln.error >= errorThreshold) break;
                            solns[i] = opt.best_soln;
                        }
                        if (i == 2)
                        {
                            var solnset = new SolutionSet(flip, diff, solns[0], solns[1]);
                            if (solnset.TotalError < bestsolns.TotalError)
                                bestsolns = solnset;
                        }

                    }
                }
                return bestsolns.ToBlock();
            }
        }
    }
}
