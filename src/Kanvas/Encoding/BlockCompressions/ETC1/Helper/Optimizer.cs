using System;
using System.Collections.Generic;
using System.Linq;
using Kanvas.Encoding.BlockCompressions.ETC1.Models;

namespace Kanvas.Encoding.BlockCompressions.ETC1.Helper
{
    // Loosely based on rg_etc1
    internal class Optimizer
    {
        private readonly RGB[] _pixels;
        private readonly int _limit;

        private RGB _baseColor;
        private Solution _bestSln;

        private Optimizer(RGB[] pixels, int limit, int error)
        {
            _pixels = pixels;
            _limit = limit;
            _baseColor = RGB.Average(pixels).Unscale(limit);
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
                                      select new RGB(x, y, z));
        }

        private IEnumerable<Solution> FindExactMatches(IEnumerable<RGB> colors, int[] intenTable)
        {
            foreach (var c in colors)
            {
                _bestSln.Error = 1;
                if (EvaluateSolution(c, intenTable))
                    yield return _bestSln;
            }
        }

        private bool TestUnscaledColors(IEnumerable<RGB> colors)
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

        private bool EvaluateSolution(RGB c, int[] intenTable)
        {
            var soln = new Solution { BlockColor = c, IntenTable = intenTable };
            var newTable = new RGB[4];
            var scaledColor = c.Scale(_limit);
            for (var i = 0; i < 4; i++)
                newTable[i] = scaledColor + intenTable[i];

            for (var i = 0; i < 8; i++)
            {
                int best_j = 0, best_error = int.MaxValue;
                for (int j = 0; j < 4; j++)
                {
                    int error = _pixels[i] - newTable[j];
                    if (error < best_error)
                    {
                        best_error = error;
                        best_j = j;
                    }
                }
                soln.Error += best_error;
                if (soln.Error >= _bestSln.Error) return false;
                soln.SelectorMSB |= (byte)(best_j / 2 << i);
                soln.SelectorLSB |= (byte)(best_j % 2 << i);
            }

            _bestSln = soln;
            return true;
        }

        #region Pre-computed lookup table for recompressing etc1
        private static readonly bool[][] _lookup16 = new bool[8][];
        private static readonly bool[][] _lookup32 = new bool[8][];
        private static readonly byte[][][] _lookup16big = new byte[8][][];
        private static readonly byte[][][] _lookup32big = new byte[8][][];

        private static int Clamp(int n) => Math.Max(0, Math.Min(n, 255));

        static Optimizer()
        {
            for (int i = 0; i < 8; i++)
            {
                _lookup16[i] = new bool[256];
                _lookup32[i] = new bool[256];
                _lookup16big[i] = new byte[16][];
                _lookup32big[i] = new byte[32][];
                for (int j = 0; j < 16; j++)
                {
                    _lookup16big[i][j] = Constants.Modifiers[i].Select(mod => (byte)Clamp(j * 17 + mod)).Distinct().ToArray();
                    foreach (var k in _lookup16big[i][j]) _lookup16[i][k] = true;
                }
                for (int j = 0; j < 32; j++)
                {
                    _lookup32big[i][j] = Constants.Modifiers[i].Select(mod => (byte)Clamp(j * 8 + j / 4 + mod)).Distinct().ToArray();
                    foreach (var k in _lookup32big[i][j]) _lookup32[i][k] = true;
                }
            }
        }
        #endregion

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
                        var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => _lookup16[i][c.R] && _lookup16[i][c.G] && _lookup16[i][c.B])).ToList();
                        if (!tables0.Any()) continue;
                        var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => _lookup16[i][c.R] && _lookup16[i][c.G] && _lookup16[i][c.B])).ToList();
                        if (!tables1.Any()) continue;

                        var opt0 = new Optimizer(allpixels0, 16, 1);
                        Solution soln0 = null;
                        foreach (var ti in tables0)
                        {
                            var rs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => _lookup16big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => _lookup16big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 16).Where(a => pixels0.All(c => _lookup16big[ti][a].Contains(c.B))).ToArray();
                            soln0 = opt0.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), Constants.Modifiers[ti]).FirstOrDefault();
                            if (soln0 != null) break;
                        }
                        if (soln0 == null) continue;

                        var opt1 = new Optimizer(allpixels1, 16, 1);
                        foreach (var ti in tables1)
                        {
                            var rs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => _lookup16big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => _lookup16big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 16).Where(a => pixels1.All(c => _lookup16big[ti][a].Contains(c.B))).ToArray();
                            var soln1 = opt1.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), Constants.Modifiers[ti]).FirstOrDefault();
                            if (soln1 != null)
                            {
                                block = new SolutionSet(flip, false, soln0, soln1).ToBlock();
                                return true;
                            }
                        }
                    }
                    else
                    {
                        var tables0 = Enumerable.Range(0, 8).Where(i => pixels0.All(c => _lookup32[i][c.R] && _lookup32[i][c.G] && _lookup32[i][c.B])).ToList();
                        if (!tables0.Any()) continue;
                        var tables1 = Enumerable.Range(0, 8).Where(i => pixels1.All(c => _lookup32[i][c.R] && _lookup32[i][c.G] && _lookup32[i][c.B])).ToList();
                        if (!tables1.Any()) continue;

                        var opt0 = new Optimizer(allpixels0, 32, 1);
                        var solns0 = new List<Solution>();
                        foreach (var ti in tables0)
                        {
                            var rs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => _lookup32big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => _lookup32big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 32).Where(a => pixels0.All(c => _lookup32big[ti][a].Contains(c.B))).ToArray();
                            solns0.AddRange(opt0.FindExactMatches(from r in rs from g in gs from b in bs select new RGB(r, g, b), Constants.Modifiers[ti]));
                        }
                        if (!solns0.Any()) continue;

                        var opt1 = new Optimizer(allpixels1, 32, 1);
                        foreach (var ti in tables1)
                        {
                            var rs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => _lookup32big[ti][a].Contains(c.R))).ToArray();
                            var gs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => _lookup32big[ti][a].Contains(c.G))).ToArray();
                            var bs = Enumerable.Range(0, 32).Where(a => pixels1.All(c => _lookup32big[ti][a].Contains(c.B))).ToArray();
                            foreach (var soln0 in solns0)
                            {
                                var q = (from r in rs
                                         let dr = r - soln0.BlockColor.R
                                         where dr >= -4 && dr < 4
                                         from g in gs
                                         let dg = g - soln0.BlockColor.G
                                         where dg >= -4 && dg < 4
                                         from b in bs
                                         let db = b - soln0.BlockColor.B
                                         where db >= -4 && db < 4
                                         select new RGB(r, g, b));
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

        public static Block Encode(List<RGB> colors)
        {
            // regular case: just try our best to compress and minimise Error
            var bestsolns = new SolutionSet();
            foreach (var flip in new[] { false, true })
            {
                var pixels = new[] { 0, 1 }.Select(i => colors.Where((c, j) => (j / (flip ? 2 : 8)) % 2 == i).ToArray()).ToArray();
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
