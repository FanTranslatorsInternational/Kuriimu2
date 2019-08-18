using System;
using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.LempelZiv;
using Kompression.Specialized.SlimeMoriMori.Decoders;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    abstract class SlimeEncoder : ISlimeEncoder
    {
        private DisplacementElement[] _displacementTable;

        public abstract void Encode(Stream input, BitWriter bw, LzMatch[] matches);

        protected void CreateDisplacementTable(long[] displacements, int entryCount)
        {
            _displacementTable = new DisplacementElement[entryCount];

            var maxDisplacement = displacements.Max();
            var entryRange = maxDisplacement / entryCount;
            var codeBits = (int)Math.Log(entryRange, 2);

            // Fill first n-1 entries
            var displacementStart = 1;
            for (var i = 0; i < entryCount - 1; i++)
            {
                _displacementTable[i] = new DisplacementElement(codeBits, displacementStart);
                displacementStart += (short)(1 << codeBits);
            }

            // Fill last entry based on current displacement start and max displacement
            var remainingRange = maxDisplacement - displacementStart;
            codeBits = (int)Math.Log(remainingRange, 2);
            if (1 << codeBits != remainingRange)
                codeBits++;
            _displacementTable[entryCount - 1] = new DisplacementElement(codeBits, displacementStart);
        }

        protected void WriteDisplacementTable(BitWriter bw)
        {
            if (_displacementTable == null)
                throw new InvalidOperationException("Displacement table has to be initialized.");

            foreach (var entry in _displacementTable)
                bw.WriteBits(entry.ReadBits, 4);
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
    }
}
