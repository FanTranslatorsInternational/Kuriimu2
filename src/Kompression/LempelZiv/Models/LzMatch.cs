namespace Kompression.LempelZiv.Models
{
    public class LzMatch
    {
        public long Position { get; }
        public long Displacement { get; }
        public int Length { get; }

        public LzMatch(long position, long displacement, int length)
        {
            Position = position;
            Displacement = displacement;
            Length = length;
        }
    }
}
