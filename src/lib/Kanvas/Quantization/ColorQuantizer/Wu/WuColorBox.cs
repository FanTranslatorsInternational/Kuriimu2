namespace Kanvas.Quantization.ColorQuantizer.Wu
{
    /// <summary>
    /// A box color 
    /// </summary>
    internal class WuColorBox(Wu3DHistogram histogram)
    {
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

        private int GetIndex(int r, int g, int b, int a)
        {
            return WuCommon.GetIndex(r, g, b, a, histogram.IndexGreenCount, histogram.IndexBlueCount, histogram.IndexAlphaCount);
        }

        private long Bottom(int direction, long[] moment)
        {
            return direction switch
            {
                // Red
                3 => -moment[GetIndex(R0, G1, B1, A1)] + moment[GetIndex(R0, G1, B1, A0)] +
                    moment[GetIndex(R0, G1, B0, A1)] - moment[GetIndex(R0, G1, B0, A0)] +
                    moment[GetIndex(R0, G0, B1, A1)] - moment[GetIndex(R0, G0, B1, A0)] -
                    moment[GetIndex(R0, G0, B0, A1)] + moment[GetIndex(R0, G0, B0, A0)],
                // Green
                2 => -moment[GetIndex(R1, G0, B1, A1)] + moment[GetIndex(R1, G0, B1, A0)] +
                    moment[GetIndex(R1, G0, B0, A1)] - moment[GetIndex(R1, G0, B0, A0)] +
                    moment[GetIndex(R0, G0, B1, A1)] - moment[GetIndex(R0, G0, B1, A0)] -
                    moment[GetIndex(R0, G0, B0, A1)] + moment[GetIndex(R0, G0, B0, A0)],
                // Blue
                1 => -moment[GetIndex(R1, G1, B0, A1)] + moment[GetIndex(R1, G1, B0, A0)] +
                    moment[GetIndex(R1, G0, B0, A1)] - moment[GetIndex(R1, G0, B0, A0)] +
                    moment[GetIndex(R0, G1, B0, A1)] - moment[GetIndex(R0, G1, B0, A0)] -
                    moment[GetIndex(R0, G0, B0, A1)] + moment[GetIndex(R0, G0, B0, A0)],
                // Alpha
                0 => -moment[GetIndex(R1, G1, B1, A0)] + moment[GetIndex(R1, G1, B0, A0)] +
                    moment[GetIndex(R1, G0, B1, A0)] - moment[GetIndex(R1, G0, B0, A0)] +
                    moment[GetIndex(R0, G1, B1, A0)] - moment[GetIndex(R0, G1, B0, A0)] -
                    moment[GetIndex(R0, G0, B1, A0)] + moment[GetIndex(R0, G0, B0, A0)],
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        private long Top(int direction, int position, long[] moment)
        {
            return direction switch
            {
                // Red
                3 => moment[GetIndex(position, G1, B1, A1)] - moment[GetIndex(position, G1, B1, A0)] -
                    moment[GetIndex(position, G1, B0, A1)] + moment[GetIndex(position, G1, B0, A0)] -
                    moment[GetIndex(position, G0, B1, A1)] + moment[GetIndex(position, G0, B1, A0)] +
                    moment[GetIndex(position, G0, B0, A1)] - moment[GetIndex(position, G0, B0, A0)],
                // Green
                2 => moment[GetIndex(R1, position, B1, A1)] - moment[GetIndex(R1, position, B1, A0)] -
                    moment[GetIndex(R1, position, B0, A1)] + moment[GetIndex(R1, position, B0, A0)] -
                    moment[GetIndex(R0, position, B1, A1)] + moment[GetIndex(R0, position, B1, A0)] +
                    moment[GetIndex(R0, position, B0, A1)] - moment[GetIndex(R0, position, B0, A0)],
                // Blue
                1 => moment[GetIndex(R1, G1, position, A1)] - moment[GetIndex(R1, G1, position, A0)] -
                    moment[GetIndex(R1, G0, position, A1)] + moment[GetIndex(R1, G0, position, A0)] -
                    moment[GetIndex(R0, G1, position, A1)] + moment[GetIndex(R0, G1, position, A0)] +
                    moment[GetIndex(R0, G0, position, A1)] - moment[GetIndex(R0, G0, position, A0)],
                // Alpha
                0 => moment[GetIndex(R1, G1, B1, position)] - moment[GetIndex(R1, G1, B0, position)] -
                    moment[GetIndex(R1, G0, B1, position)] + moment[GetIndex(R1, G0, B0, position)] -
                    moment[GetIndex(R0, G1, B1, position)] + moment[GetIndex(R0, G1, B0, position)] +
                    moment[GetIndex(R0, G0, B1, position)] - moment[GetIndex(R0, G0, B0, position)],
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public double Maximize(int direction, int first, int last, out int cut, double wholeR, double wholeG, double wholeB, double wholeA, double wholeW)
        {
            long baseR = Bottom(direction, histogram.Vmr);
            long baseG = Bottom(direction, histogram.Vmg);
            long baseB = Bottom(direction, histogram.Vmb);
            long baseA = Bottom(direction, histogram.Vma);
            long baseW = Bottom(direction, histogram.Vwt);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                double halfR = baseR + Top(direction, i, histogram.Vmr);
                double halfG = baseG + Top(direction, i, histogram.Vmg);
                double halfB = baseB + Top(direction, i, histogram.Vmb);
                double halfA = baseA + Top(direction, i, histogram.Vma);
                double halfW = baseW + Top(direction, i, histogram.Vwt);

                if (halfW == 0)
                {
                    continue;
                }

                double temp = (halfR * halfR + halfG * halfG + halfB * halfB + halfA * halfA) / halfW;

                halfR = wholeR - halfR;
                halfG = wholeG - halfG;
                halfB = wholeB - halfB;
                halfA = wholeA - halfA;
                halfW = wholeW - halfW;

                if (halfW == 0)
                {
                    continue;
                }

                temp += (halfR * halfR + halfG * halfG + halfB * halfB + halfA * halfA) / halfW;

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
            long[] moment = direction switch
            {
                1 => histogram.Vmr,
                2 => histogram.Vmg,
                3 => histogram.Vmb,
                4 => histogram.Vma,
                5 => histogram.Vwt,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };

            return moment[GetIndex(R1, G1, B1, A1)]
                   - moment[GetIndex(R1, G1, B1, A0)]
                   - moment[GetIndex(R1, G1, B0, A1)]
                   + moment[GetIndex(R1, G1, B0, A0)]
                   - moment[GetIndex(R1, G0, B1, A1)]
                   + moment[GetIndex(R1, G0, B1, A0)]
                   + moment[GetIndex(R1, G0, B0, A1)]
                   - moment[GetIndex(R1, G0, B0, A0)]
                   - moment[GetIndex(R0, G1, B1, A1)]
                   + moment[GetIndex(R0, G1, B1, A0)]
                   + moment[GetIndex(R0, G1, B0, A1)]
                   - moment[GetIndex(R0, G1, B0, A0)]
                   + moment[GetIndex(R0, G0, B1, A1)]
                   - moment[GetIndex(R0, G0, B1, A0)]
                   - moment[GetIndex(R0, G0, B0, A1)]
                   + moment[GetIndex(R0, G0, B0, A0)];
        }

        public double GetVariance()
        {
            double dr = GetPartialVolume(1);
            double dg = GetPartialVolume(2);
            double db = GetPartialVolume(3);
            double da = GetPartialVolume(4);

            double xx =
                histogram.M2[GetIndex(R1, G1, B1, A1)]
                - histogram.M2[GetIndex(R1, G1, B1, A0)]
                - histogram.M2[GetIndex(R1, G1, B0, A1)]
                + histogram.M2[GetIndex(R1, G1, B0, A0)]
                - histogram.M2[GetIndex(R1, G0, B1, A1)]
                + histogram.M2[GetIndex(R1, G0, B1, A0)]
                + histogram.M2[GetIndex(R1, G0, B0, A1)]
                - histogram.M2[GetIndex(R1, G0, B0, A0)]
                - histogram.M2[GetIndex(R0, G1, B1, A1)]
                + histogram.M2[GetIndex(R0, G1, B1, A0)]
                + histogram.M2[GetIndex(R0, G1, B0, A1)]
                - histogram.M2[GetIndex(R0, G1, B0, A0)]
                + histogram.M2[GetIndex(R0, G0, B1, A1)]
                - histogram.M2[GetIndex(R0, G0, B1, A0)]
                - histogram.M2[GetIndex(R0, G0, B0, A1)]
                + histogram.M2[GetIndex(R0, G0, B0, A0)];

            return xx - (dr * dr + dg * dg + db * db + da * da) / GetPartialVolume(5);
        }
    }
}
