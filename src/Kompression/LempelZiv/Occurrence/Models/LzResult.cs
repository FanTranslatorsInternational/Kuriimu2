namespace Kompression.LempelZiv.Occurrence.Models
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
