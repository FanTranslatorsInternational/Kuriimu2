using System;
using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.PatternMatch;
using Kompression.Specialized.SlimeMoriMori.Decoders;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    abstract class SlimeEncoder : ISlimeEncoder
    {
        private DisplacementElement[] _displacementTable;

        public abstract void Encode(Stream input, BitWriter bw, Match[] matches);

        /// <summary>
        /// Initializes and fills the displacement table.
        /// </summary>
        /// <param name="displacements">Displacements from all found matches.</param>
        /// <param name="entryCount">The number of entries in the final table.</param>
        protected void CreateDisplacementTable(long[] displacements, int entryCount)
        {
            var distribution = CalculateDisplacementCoverage(displacements.Select(x => (double)x).ToArray(), entryCount);
            _displacementTable = new DisplacementElement[entryCount];

            var displacementStart = 1;
            var codeBits = (int)Math.Log(distribution[0], 2);
            for (var i = 0; i < entryCount - 1; i++)
            {
                _displacementTable[i] = new DisplacementElement(codeBits, displacementStart);
                displacementStart += (short)(1 << codeBits);
                codeBits = (int)Math.Log(distribution[i + 1] - displacementStart, 2);
            }

            if (1 << codeBits != (int)distribution[entryCount - 1] - displacementStart)
                codeBits++;

            _displacementTable[entryCount - 1] = new DisplacementElement(codeBits, displacementStart);
        }

        /// <summary>
        /// Calculates the coverage of the used displacements in percentile ranges.
        /// </summary>
        /// <param name="displacements">The list of displacements.</param>
        /// <param name="rangeCount">The percentile ranges.</param>
        /// <returns>The max value of each coverage percentile.</returns>
        private double[] CalculateDisplacementCoverage(double[] displacements, int rangeCount)
        {
            var percentiles = new double[rangeCount];
            var range = 1d / rangeCount;
            percentiles[0] = range;
            percentiles[rangeCount - 1] = 1;
            for (var i = 1; i < rangeCount - 1; i++)
                percentiles[i] = percentiles[i - 1] + range;

            var result = new double[rangeCount];
            for (var i = 0; i < percentiles.Length; i++)
                result[i] = CalculatePercentile(displacements, percentiles[i]);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="excelPercentile"></param>
        /// <returns></returns>
        /// <remarks>https://stackoverflow.com/questions/8137391/percentile-calculation</remarks>
        private double CalculatePercentile(double[] sequence, double excelPercentile)
        {
            Array.Sort(sequence);
            int N = sequence.Length;
            double n = (N - 1) * excelPercentile + 1;

            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d)
                return sequence[0];

            if (n == N)
                return sequence[N - 1];

            int k = (int)n;
            double d = n - k;
            return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
        }

        protected void WriteDisplacementTable(BitWriter bw)
        {
            if (_displacementTable == null)
                throw new InvalidOperationException("Displacement table has to be initialized.");

            foreach (var entry in _displacementTable)
                bw.WriteBits(entry.ReadBits - 1, 4);
        }

        protected int GetDisplacementIndex(long displacement)
        {
            var index = 0;
            for (var i = 1; i < _displacementTable.Length; i++)
                if (displacement >= _displacementTable[i].DisplacementStart)
                    index = i;
                else
                    break;

            return index;
        }

        protected DisplacementElement GetDisplacementEntry(int dispIndex)
        {
            return _displacementTable[dispIndex];
        }
    }
}
