using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv
{
    class LzResult
    {
        public long Position { get; }
        public long Displacement { get; }
        public int Length { get; }
        public byte[] DiscrepancyBuffer { get; }

        public LzResult(long position, long displacement, int length, byte[] discrepancyBuffer)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
            DiscrepancyBuffer = discrepancyBuffer;
        }
    }
}
