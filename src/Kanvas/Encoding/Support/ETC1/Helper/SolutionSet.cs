using System;
using Kanvas.Encoding.Support.ETC1.Models;

namespace Kanvas.Encoding.Support.ETC1.Helper
{
    internal class SolutionSet
    {
        private const int MAX_ERROR = 99999999;

        private readonly bool _flip;
        private readonly bool _diff;
        private readonly Solution _soln0;
        private readonly Solution _soln1;

        public int TotalError => _soln0.Error + _soln1.Error;

        public SolutionSet()
        {
            _soln1 = _soln0 = new Solution { Error = MAX_ERROR };
        }

        public SolutionSet(bool flip, bool diff, Solution soln0, Solution soln1)
        {
            _flip = flip;
            _diff = diff;
            _soln0 = soln0;
            _soln1 = soln1;
        }

        public Block ToBlock()
        {
            var blk = new Block
            {
                DiffBit = _diff,
                FlipBit = _flip,
                Table0 = Array.IndexOf(Constants.Modifiers, _soln0.IntenTable),
                Table1 = Array.IndexOf(Constants.Modifiers, _soln1.IntenTable)
            };

            if (blk.FlipBit)
            {
                int m0 = _soln0.SelectorMSB, m1 = _soln1.SelectorMSB;
                m0 = (m0 & 0xC0) * 64 + (m0 & 0x30) * 16 + (m0 & 0xC) * 4 + (m0 & 0x3);
                m1 = (m1 & 0xC0) * 64 + (m1 & 0x30) * 16 + (m1 & 0xC) * 4 + (m1 & 0x3);
                blk.MSB = (ushort)(m0 + 4 * m1);
                int l0 = _soln0.SelectorLSB, l1 = _soln1.SelectorLSB;
                l0 = (l0 & 0xC0) * 64 + (l0 & 0x30) * 16 + (l0 & 0xC) * 4 + (l0 & 0x3);
                l1 = (l1 & 0xC0) * 64 + (l1 & 0x30) * 16 + (l1 & 0xC) * 4 + (l1 & 0x3);
                blk.LSB = (ushort)(l0 + 4 * l1);
            }
            else
            {
                blk.MSB = (ushort)(_soln0.SelectorMSB + 256 * _soln1.SelectorMSB);
                blk.LSB = (ushort)(_soln0.SelectorLSB + 256 * _soln1.SelectorLSB);
            }

            if (blk.DiffBit)
            {
                int rdiff = (_soln1.BlockColor.R - _soln0.BlockColor.R + 8) % 8;
                int gdiff = (_soln1.BlockColor.G - _soln0.BlockColor.G + 8) % 8;
                int bdiff = (_soln1.BlockColor.B - _soln0.BlockColor.B + 8) % 8;
                blk.R = (byte)(_soln0.BlockColor.R * 8 + rdiff);
                blk.G = (byte)(_soln0.BlockColor.G * 8 + gdiff);
                blk.B = (byte)(_soln0.BlockColor.B * 8 + bdiff);
            }
            else
            {
                blk.R = (byte)(_soln0.BlockColor.R * 16 + _soln1.BlockColor.R);
                blk.G = (byte)(_soln0.BlockColor.G * 16 + _soln1.BlockColor.G);
                blk.B = (byte)(_soln0.BlockColor.B * 16 + _soln1.BlockColor.B);
            }

            return blk;
        }
    }
}
