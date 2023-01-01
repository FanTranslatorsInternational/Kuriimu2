using System;

namespace Kanvas.Quantization.Models.Quantizer.Wu
{
    /// <summary>
    /// A box color 
    /// </summary>
    class WuColorBox
    {
        private readonly Wu3DHistogram _histogram;

        /// <summary>
        /// Gets or sets the min red value, exclusive.
        /// </summary>
        public int R0 { get; set; }

        /// <summary>
        /// Gets or sets the max red value, inclusive.
        /// </summary>
        public int R1 { get; set; }

        /// <summary>
        /// Gets or sets the min green value, exclusive.
        /// </summary>
        public int G0 { get; set; }

        /// <summary>
        /// Gets or sets the max green value, inclusive.
        /// </summary>
        public int G1 { get; set; }

        /// <summary>
        /// Gets or sets the min blue value, exclusive.
        /// </summary>
        public int B0 { get; set; }

        /// <summary>
        /// Gets or sets the max blue value, inclusive.
        /// </summary>
        public int B1 { get; set; }

        /// <summary>
        /// Gets or sets the min alpha value, exclusive.
        /// </summary>
        public int A0 { get; set; }

        /// <summary>
        /// Gets or sets the max alpha value, inclusive.
        /// </summary>
        public int A1 { get; set; }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        public int Volume { get; set; }

        public WuColorBox(Wu3DHistogram histogram)
        {
            _histogram = histogram;
        }

        private long Bottom(int direction, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return -moment[WuCommon.GetIndex(R0, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Green
                case 2:
                    return -moment[WuCommon.GetIndex(R1, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Blue
                case 1:
                    return -moment[WuCommon.GetIndex(R1, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Alpha
                case 0:
                    return -moment[WuCommon.GetIndex(R1, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private long Top(int direction, int position, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return moment[WuCommon.GetIndex(position, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(position, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(position, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(position, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(position, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(position, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(position, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(position, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Green
                case 2:
                    return moment[WuCommon.GetIndex(R1, position, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, position, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, position, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, position, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, position, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, position, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, position, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, position, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Blue
                case 1:
                    return moment[WuCommon.GetIndex(R1, G1, position, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G1, position, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G0, position, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, position, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G1, position, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, position, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, position, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, position, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                // Alpha
                case 0:
                    return moment[WuCommon.GetIndex(R1, G1, B1, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G1, B0, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R1, G0, B1, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R1, G0, B0, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G1, B1, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G1, B0, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        + moment[WuCommon.GetIndex(R0, G0, B1, position, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                        - moment[WuCommon.GetIndex(R0, G0, B0, position, _histogram.IndexBits, _histogram.IndexAlphaBits)];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public double Maximize(int direction, int first, int last, out int cut, double wholeR, double wholeG, double wholeB, double wholeA, double wholeW)
        {
            long baseR = Bottom(direction, _histogram.Vmr);
            long baseG = Bottom(direction, _histogram.Vmg);
            long baseB = Bottom(direction, _histogram.Vmb);
            long baseA = Bottom(direction, _histogram.Vma);
            long baseW = Bottom(direction, _histogram.Vwt);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                double halfR = baseR + Top(direction, i, _histogram.Vmr);
                double halfG = baseG + Top(direction, i, _histogram.Vmg);
                double halfB = baseB + Top(direction, i, _histogram.Vmb);
                double halfA = baseA + Top(direction, i, _histogram.Vma);
                double halfW = baseW + Top(direction, i, _histogram.Vwt);

                if (halfW == 0)
                {
                    continue;
                }

                double temp = ((halfR * halfR) + (halfG * halfG) + (halfB * halfB) + (halfA * halfA)) / halfW;

                halfR = wholeR - halfR;
                halfG = wholeG - halfG;
                halfB = wholeB - halfB;
                halfA = wholeA - halfA;
                halfW = wholeW - halfW;

                if (halfW == 0)
                {
                    continue;
                }

                temp += ((halfR * halfR) + (halfG * halfG) + (halfB * halfB) + (halfA * halfA)) / halfW;

                if (temp > max)
                {
                    max = temp;
                    cut = i;
                }
            }

            return max;
        }

        public double GetPartialVolume(int direction)
        {
            long[] moment;
            switch (direction)
            {
                case 1:
                    moment = _histogram.Vmr;
                    break;
                case 2:
                    moment = _histogram.Vmg;
                    break;
                case 3:
                    moment = _histogram.Vmb;
                    break;
                case 4:
                    moment = _histogram.Vma;
                    break;
                case 5:
                    moment = _histogram.Vwt;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }

            return moment[WuCommon.GetIndex(R1, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R1, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R1, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R1, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R1, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R1, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R1, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R1, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R0, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R0, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R0, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R0, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R0, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R0, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   - moment[WuCommon.GetIndex(R0, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                   + moment[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];
        }

        public double GetVariance()
        {
            double dr = GetPartialVolume(1);
            double dg = GetPartialVolume(2);
            double db = GetPartialVolume(3);
            double da = GetPartialVolume(4);

            double xx =
                _histogram.M2[WuCommon.GetIndex(R1, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R1, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R1, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R1, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R1, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R1, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R1, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R1, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R0, G1, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R0, G1, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R0, G1, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R0, G1, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R0, G0, B1, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R0, G0, B1, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                - _histogram.M2[WuCommon.GetIndex(R0, G0, B0, A1, _histogram.IndexBits, _histogram.IndexAlphaBits)]
                + _histogram.M2[WuCommon.GetIndex(R0, G0, B0, A0, _histogram.IndexBits, _histogram.IndexAlphaBits)];

            return xx - (((dr * dr) + (dg * dg) + (db * db) + (da * da)) / GetPartialVolume(5));
        }
    }
}
