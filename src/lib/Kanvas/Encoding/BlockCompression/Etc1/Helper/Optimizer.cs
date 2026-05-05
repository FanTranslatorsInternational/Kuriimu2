using Kanvas.Encoding.BlockCompression.Etc1.Models;

namespace Kanvas.Encoding.BlockCompression.Etc1.Helper
{
    // Loosely based on rg_etc1
    internal class Optimizer
    {
        private readonly Rgb[] _pixels;
        private readonly int _limit;

        private Rgb _baseColor;
        private Solution _bestSln;

        private Optimizer(Rgb[] pixels, int limit, int error)
        {
            _pixels = pixels;
            _limit = limit;
            _baseColor = Rgb.Average(pixels).Unscale(limit);
            _bestSln = new Solution { Error = error };
        }

        private bool ComputeDeltas(params int[] deltas)
        {
            return TestUnscaledColors(from zd in deltas
                                      let z = zd + _baseColor.B
                                      where z >= 0 && z < _limit
                                      from yd in deltas
                                      let y = yd + _baseColor.G
                                      where y >= 0 && y < _limit
                                      from xd in deltas
                                      let x = xd + _baseColor.R
                                      where x >= 0 && x < _limit
                                      select new Rgb(x, y, z));
        }

        private IEnumerable<Solution> FindExactMatches(IEnumerable<Rgb> colors, int[] intenTable)
        {
            foreach (var c in colors)
            {
                _bestSln.Error = 1;
                if (EvaluateSolution(c, intenTable))
                    yield return _bestSln;
            }
        }

        private bool TestUnscaledColors(IEnumerable<Rgb> colors)
        {
            var success = false;
            foreach (var c in colors)
            {
                foreach (var t in Constants.Modifiers)
                {
                    if (!EvaluateSolution(c, t))
                        continue;

                    success = true;
                    if (_bestSln.Error == 0) return true;
                }
            }
            return success;
        }

        private bool EvaluateSolution(Rgb c, int[] intenTable)
        {
            var soln = new Solution { BlockColor = c, IntenTable = intenTable };
            var newTable = new Rgb[4];
            var scaledColor = c.Scale(_limit);
            for (var i = 0; i < 4; i++)
                newTable[i] = scaledColor + intenTable[i];

            for (var i = 0; i < 8; i++)
            {
                int bestJ = 0, bestError = int.MaxValue;
                for (int j = 0; j < 4; j++)
                {
                    int error = _pixels[i] - newTable[j];
                    if (error < bestError)
                    {
                        bestError = error;
                        bestJ = j;
                    }
                }
                soln.Error += bestError;
                if (soln.Error >= _bestSln.Error) return false;
                soln.SelectorMsb |= (byte)(bestJ / 2 << i);
                soln.SelectorLsb |= (byte)(bestJ % 2 << i);
            }

            _bestSln = soln;
            return true;
        }

        #region Pre-computed lookup table for recompressing etc1
        private static readonly bool[][] Lookup16 = new bool[8][];
        private static readonly bool[][] Lookup32 = new bool[8][];
        private static readonly byte[][][] Lookup16Big = new byte[8][][];
        private static readonly byte[][][] Lookup32Big = new byte[8][][];

        private static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));

        static Optimizer()
        {
            for (int i = 0; i < 8; i++)
            {
                Lookup16[i] = new bool[256];
                Lookup32[i] = new bool[256];
                Lookup16Big[i] = new byte[16][];
                Lookup32Big[i] = new byte[32][];
                for (int j = 0; j < 16; j++)
                {
                    int j1 = j;
                    Lookup16Big[i][j] = [.. Constants.Modifiers[i].Select(mod => (byte)Clamp(j1 * 17 + mod)).Distinct()];
                    foreach (var k in Lookup16Big[i][j]) Lookup16[i][k] = true;
                }
                for (int j = 0; j < 32; j++)
                {
                    int j1 = j;
                    Lookup32Big[i][j] = [.. Constants.Modifiers[i].Select(mod => (byte)Clamp(j1 * 8 + j1 / 4 + mod)).Distinct()];
                    foreach (var k in Lookup32Big[i][j]) Lookup32[i][k] = true;
                }
            }
        }
        #endregion

        public static bool RepackEtc1CompressedBlock(List<Rgb> colors, out Block block)
        {
            foreach (var flip in new[] { false, true })
            {
                var allpixels0 = colors.Where((_, j) => j / (flip ? 2 : 8) % 2 == 0).ToArray();
                var pixels0 = allpixels0.Distinct().ToArray();
                if (pixels0.Length > 4) continue;

                var allpixels1 = colors.Where((_, j) => j / (flip ? 2 : 8) % 2 == 1).ToArray();
                var pixels1 = allpixels1.Distinct().ToArray();
                if (pixels1.Length > 4) continue;

                foreach (var diff in new[] { false, true })
                {
                    if (!diff)
                    {
                        var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => Lookup16[i][c.R] && Lookup16[i][c.G] && Lookup16[i][c.B])).ToList();
                        if (tables0.Count <= 0) continue;
                        var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => Lookup16[i][c.R] && Lookup16[i][c.G] && Lookup16[i][c.B])).ToList();
                        if (tables1.Count <= 0) continue;

                        var opt0 = new Optimizer(allpixels0, 16, 1);
                        Solution? soln0 = null;
                        foreach (var ti in tables0)
                        {
                            var rs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => Lookup16Big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => Lookup16Big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => Lookup16Big[ti][a].Contains(c.B))).ToArray();
                            soln0 = opt0.FindExactMatches(from r in rs from g in gs from b in bs select new Rgb(r, g, b), Constants.Modifiers[ti]).FirstOrDefault();
                            if (soln0 != null) break;
                        }
                        if (soln0 == null) continue;

                        var opt1 = new Optimizer(allpixels1, 16, 1);
                        foreach (var ti in tables1)
                        {
                            var rs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => Lookup16Big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => Lookup16Big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => Lookup16Big[ti][a].Contains(c.B))).ToArray();
                            var soln1 = opt1.FindExactMatches(from r in rs from g in gs from b in bs select new Rgb(r, g, b), Constants.Modifiers[ti]).FirstOrDefault();
                            if (soln1 != null)
                            {
                                block = new SolutionSet(flip, false, soln0, soln1).ToBlock();
                                return true;
                            }
                        }
                    }
                    else
                    {
                        var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => Lookup32[i][c.R] && Lookup32[i][c.G] && Lookup32[i][c.B])).ToList();
                        if (tables0.Count <= 0) continue;
                        var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => Lookup32[i][c.R] && Lookup32[i][c.G] && Lookup32[i][c.B])).ToList();
                        if (tables1.Count <= 0) continue;

                        var opt0 = new Optimizer(allpixels0, 32, 1);
                        var solns0 = new List<Solution>();
                        foreach (var ti in tables0)
                        {
                            var rs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => Lookup32Big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => Lookup32Big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => Lookup32Big[ti][a].Contains(c.B))).ToArray();
                            solns0.AddRange(opt0.FindExactMatches(from r in rs from g in gs from b in bs select new Rgb(r, g, b), Constants.Modifiers[ti]));
                        }
                        if (solns0.Count <= 0) continue;

                        var opt1 = new Optimizer(allpixels1, 32, 1);
                        foreach (var ti in tables1)
                        {
                            var rs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => Lookup32Big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => Lookup32Big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => Lookup32Big[ti][a].Contains(c.B))).ToArray();
                            foreach (var soln0 in solns0)
                            {
                                var q = from r in rs
                                        let dr = r - soln0.BlockColor.R
                                        where dr >= -4 && dr < 4
                                        from g in gs
                                        let dg = g - soln0.BlockColor.G
                                        where dg >= -4 && dg < 4
                                        from b in bs
                                        let db = b - soln0.BlockColor.B
                                        where db >= -4 && db < 4
                                        select new Rgb(r, g, b);
                                var soln1 = opt1.FindExactMatches(q, Constants.Modifiers[ti]).FirstOrDefault();
                                if (soln1 != null)
                                {
                                    block = new SolutionSet(flip, true, soln0, soln1).ToBlock();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            block = new Block();
            return false;
        }

        public static Block Encode(List<Rgb> colors)
        {
            // regular case: just try our best to compress and minimise Error
            var bestsolns = new SolutionSet();
            foreach (var flip in new[] { false, true })
            {
                var pixels = Enumerable.Range(0, 2).Select(i => colors.Where((_, j) => j / (flip ? 2 : 8) % 2 == i).ToArray()).ToArray();
                foreach (var diff in new[] { false, true }) // let's again just assume no diff
                {
                    var solns = new Solution[2];
                    var limit = diff ? 32 : 16;

                    int i;
                    for (i = 0; i < 2; i++)
                    {
                        var errorThreshold = bestsolns.TotalError;
                        if (i == 1) errorThreshold -= solns[0].Error;
                        var opt = new Optimizer(pixels[i], limit, errorThreshold);
                        if (i == 1 && diff)
                        {
                            opt._baseColor = solns[0].BlockColor;
                            if (!opt.ComputeDeltas(-4, -3, -2, -1, 0, 1, 2, 3)) break;
                        }
                        else
                        {
                            if (!opt.ComputeDeltas(-4, -3, -2, -1, 0, 1, 2, 3, 4)) break;

                            // Fix fairly arbitrary/unrefined thresholds that control how far away to scan for potentially better solutions.
                            if (opt._bestSln.Error > 9000)
                            {
                                if (opt._bestSln.Error > 18000)
                                    opt.ComputeDeltas(-8, -7, -6, -5, 5, 6, 7, 8);
                                else
                                    opt.ComputeDeltas(-5, 5);
                            }
                        }
                        if (opt._bestSln.Error >= errorThreshold) break;
                        solns[i] = opt._bestSln;
                    }

                    if (i != 2)
                        continue;

                    var solnset = new SolutionSet(flip, diff, solns[0], solns[1]);
                    if (solnset.TotalError < bestsolns.TotalError)
                        bestsolns = solnset;

                }
            }
            return bestsolns.ToBlock();
        }
    }
}
